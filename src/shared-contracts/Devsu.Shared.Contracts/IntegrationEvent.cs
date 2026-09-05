namespace Devsu.Shared.Contracts;


public sealed record IntegrationEvent<TData>
{

    public Guid EventId { get; init; } = Guid.NewGuid();


    public required string EventType { get; init; }


    public int EventVersion { get; init; } = 1;


    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;


    public required string CorrelationId { get; init; }

    public required TData Data { get; init; }
}
