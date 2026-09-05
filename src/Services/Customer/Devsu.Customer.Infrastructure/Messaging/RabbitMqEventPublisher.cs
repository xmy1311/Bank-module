using System.Text;
using System.Text.Json;
using Devsu.Customer.Application.Interfaces;
using Devsu.Shared.Contracts;
using Devsu.Shared.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Devsu.Customer.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnection _conexion;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(IRabbitMqConnection conexion, ILogger<RabbitMqEventPublisher> logger)
    {
        _conexion = conexion;
        _logger = logger;
    }

    public async Task PublishAsync<TData>(
        IntegrationEvent<TData> evento,
        string routingKey,
        CancellationToken ct)
    {

        await using var canal = await _conexion.CrearCanalAsync(ct);

        var propiedades = new BasicProperties
        {
            ContentType = "application/json",
            MessageId = evento.EventId.ToString(),
            CorrelationId = evento.CorrelationId,
            Type = evento.EventType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Persistent = true
        };

        var cuerpo = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento, Opciones));

        await canal.BasicPublishAsync(
            exchange: EventosClienteExchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: propiedades,
            body: cuerpo,
            cancellationToken: ct);

        _logger.LogInformation(
            "Evento {EventType} publicado. routingKey={RoutingKey} eventId={EventId} correlationId={CorrelationId}",
            evento.EventType, routingKey, evento.EventId, evento.CorrelationId);
    }

    private const string EventosClienteExchange = Devsu.Shared.Contracts.Clientes.EventosCliente.Exchange;
}
