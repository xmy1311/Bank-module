using RabbitMQ.Client;

namespace Devsu.Shared.Messaging;


public interface IRabbitMqConnection : IAsyncDisposable
{
    bool EstaConectado { get; }

    Task<IConnection> ObtenerAsync(CancellationToken ct);

    Task<IChannel> CrearCanalAsync(CancellationToken ct);
}
