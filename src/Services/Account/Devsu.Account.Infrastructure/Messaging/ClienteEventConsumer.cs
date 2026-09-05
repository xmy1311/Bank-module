using System.Text;
using System.Text.Json;
using Devsu.Account.Application.Integracion;
using Devsu.Shared.Contracts;
using Devsu.Shared.Contracts.Clientes;
using Devsu.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Devsu.Account.Infrastructure.Messaging;

public sealed class ClienteEventConsumer : BackgroundService
{
    private const int MaxIntentos = 3;

    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnection _conexion;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClienteEventConsumer> _logger;

    private IChannel? _canal;

    public ClienteEventConsumer(
        IRabbitMqConnection conexion,
        IServiceScopeFactory scopeFactory,
        ILogger<ClienteEventConsumer> logger)
    {
        _conexion = conexion;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _canal = await _conexion.CrearCanalAsync(stoppingToken);

            await _canal.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: stoppingToken);

            var consumidor = new AsyncEventingBasicConsumer(_canal);
            consumidor.ReceivedAsync += (_, ea) => ProcesarAsync(ea, stoppingToken);

            await _canal.BasicConsumeAsync(
                queue: EventosCliente.Queue,
                autoAck: false,
                consumer: consumidor,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Consumidor escuchando la cola {Cola}.", EventosCliente.Queue);

            // Mantiene vivo el servicio hasta que se detenga la aplicación.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumidor detenido.");
        }
        catch (Exception ex)
        {
            
            _logger.LogError(
                ex,
                "El consumidor de eventos no pudo iniciarse. La réplica de clientes " +
                "no se sincronizará hasta que RabbitMQ vuelva a estar accesible.");
        }
    }

    private async Task ProcesarAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (_canal is null)
        {
            return;
        }

        IntegrationEvent<ClienteEventData>? evento;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            evento = JsonSerializer.Deserialize<IntegrationEvent<ClienteEventData>>(json, Opciones);

            if (evento is null || evento.Data is null)
            {
                throw new JsonException("El evento se deserializó como nulo.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Mensaje con formato inválido en {Cola}. Se envía a la DLQ sin reintentar. deliveryTag={Tag}",
                EventosCliente.Queue,
                ea.DeliveryTag);

            await _canal.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);

            return;
        }

        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sincronizador = scope.ServiceProvider.GetRequiredService<IClienteSyncService>();

                var resultado = await sincronizador.AplicarAsync(evento, ct);

                // ACK sólo DESPUÉS del commit. Si el proceso muere antes de
                // llegar aquí, el mensaje vuelve a la cola.
                await _canal.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);

                if (resultado != ResultadoSincronizacion.Aplicado)
                {
                    _logger.LogDebug("Evento {EventId} resuelto como {Resultado}.", evento.EventId, resultado);
                }

                return;
            }
            catch (Exception ex) when (intento < MaxIntentos)
            {
                var espera = TimeSpan.FromSeconds(Math.Pow(2, intento));

                _logger.LogWarning(
                    ex,
                    "Fallo al aplicar el evento {EventId} (intento {Intento}/{Max}). Reintento en {Espera}s.",
                    evento.EventId, intento, MaxIntentos, espera.TotalSeconds);

                await Task.Delay(espera, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Agotados los {Max} intentos para el evento {EventId}. Se envía a la DLQ.",
                    MaxIntentos, evento.EventId);

                // requeue:false -> el mensaje va al dead-letter  en lugar
                // de volver a la cola y bloquearla indefinidamente.
                await _canal.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);

                return;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_canal is not null)
        {
            await _canal.CloseAsync(cancellationToken);
            await _canal.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
