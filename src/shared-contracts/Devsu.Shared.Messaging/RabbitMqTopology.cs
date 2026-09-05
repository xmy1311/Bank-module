using Devsu.Shared.Contracts.Clientes;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Devsu.Shared.Messaging;

public static class RabbitMqTopology
{
    public static async Task DeclararAsync(IChannel canal, ILogger logger, CancellationToken ct)
    {
      
        await canal.ExchangeDeclareAsync(
            exchange: EventosCliente.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await canal.ExchangeDeclareAsync(
            exchange: EventosCliente.ExchangeDlx,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await canal.QueueDeclareAsync(
            queue: EventosCliente.QueueDlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await canal.QueueBindAsync(
            queue: EventosCliente.QueueDlq,
            exchange: EventosCliente.ExchangeDlx,
            routingKey: string.Empty,
            arguments: null,
            cancellationToken: ct);

        // Los mensajes rechazados con requeue:false van al DLX en vez de perderse.
        await canal.QueueDeclareAsync(
            queue: EventosCliente.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = EventosCliente.ExchangeDlx
            },
            cancellationToken: ct);

        await canal.QueueBindAsync(
            queue: EventosCliente.Queue,
            exchange: EventosCliente.Exchange,
            routingKey: EventosCliente.BindingPattern,
            arguments: null,
            cancellationToken: ct);

        logger.LogInformation(
            "Topología declarada: exchange {Exchange} -> cola {Cola} (DLQ {Dlq}).",
            EventosCliente.Exchange, EventosCliente.Queue, EventosCliente.QueueDlq);
    }
}
