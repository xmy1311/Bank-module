using Devsu.Shared.Contracts;

namespace Devsu.Customer.Application.Interfaces;

/// <summary>
/// Publicación de eventos de integración.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TData>(IntegrationEvent<TData> evento, string routingKey, CancellationToken ct);
}
