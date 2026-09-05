using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Devsu.Shared.Messaging;

public sealed class RabbitMqConnection : IRabbitMqConnection
{
    private readonly RabbitMqOptions _opciones;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private IConnection? _conexion;

    public RabbitMqConnection(IOptions<RabbitMqOptions> opciones, ILogger<RabbitMqConnection> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConectado => _conexion is { IsOpen: true };

    public async Task<IConnection> ObtenerAsync(CancellationToken ct)
    {
        if (EstaConectado)
        {
            return _conexion!;
        }

        // Un solo hilo abre la conexión; el resto espera y reutiliza la misma.
        await _semaforo.WaitAsync(ct);

        try
        {
            if (EstaConectado)
            {
                return _conexion!;
            }

            _conexion = await ConectarConReintentosAsync(ct);

            return _conexion;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task<IChannel> CrearCanalAsync(CancellationToken ct)
    {
        var conexion = await ObtenerAsync(ct);

        return await conexion.CreateChannelAsync(cancellationToken: ct);
    }


    private async Task<IConnection> ConectarConReintentosAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opciones.Host,
            Port = _opciones.Port,
            UserName = _opciones.Usuario,
            Password = _opciones.Password,
            VirtualHost = _opciones.VirtualHost,

            // Recuperación automática si la conexión se cae DESPUÉS de establecida.
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        for (var intento = 1; ; intento++)
        {
            try
            {
                var conexion = await factory.CreateConnectionAsync(ct);

                _logger.LogInformation(
                    "Conectado a RabbitMQ en {Host}:{Puerto} (intento {Intento}).",
                    _opciones.Host, _opciones.Port, intento);

          
                await using (var canal = await conexion.CreateChannelAsync(cancellationToken: ct))
                {
                    await RabbitMqTopology.DeclararAsync(canal, _logger, ct);
                }

                return conexion;
            }
            catch (Exception ex) when (intento < _opciones.MaxIntentosConexion && !ct.IsCancellationRequested)
            {
                var espera = TimeSpan.FromSeconds(Math.Min(_opciones.SegundosBackoffBase * intento, 30));

                _logger.LogWarning(
                    "No se pudo conectar a RabbitMQ en {Host}:{Puerto} (intento {Intento}/{Max}): {Mensaje}. " +
                    "Reintentando en {Espera}s.",
                    _opciones.Host, _opciones.Port, intento, _opciones.MaxIntentosConexion, ex.Message,
                    espera.TotalSeconds);

                await Task.Delay(espera, ct);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_conexion is not null)
        {
            await _conexion.CloseAsync();
            await _conexion.DisposeAsync();
        }

        _semaforo.Dispose();
    }
}
