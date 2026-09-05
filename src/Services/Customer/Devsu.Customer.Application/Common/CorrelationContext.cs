namespace Devsu.Customer.Application.Common;

/// <summary>
/// Lleva el correlation ID de la petición hasta el publicador de eventos, para
/// poder trazar la operación cruzando el broker. Scoped.
/// </summary>
public sealed class CorrelationContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
