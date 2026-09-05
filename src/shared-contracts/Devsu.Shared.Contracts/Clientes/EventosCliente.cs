namespace Devsu.Shared.Contracts.Clientes;

/// <summary>
/// Nombres de evento y routing keys. Constantes compartidas para que
/// publicador y consumidor no puedan desincronizarse por un typo.
/// </summary>
public static class EventosCliente
{
    public const string Exchange = "devsu.clientes";
    public const string ExchangeDlx = "devsu.clientes.dlx";
    public const string Queue = "account.clientes.sync";
    public const string QueueDlq = "account.clientes.sync.dlq";
    public const string BindingPattern = "cliente.*";

    public const string CreadoTipo = "ClienteCreado";
    public const string CreadoRoutingKey = "cliente.creado";

    public const string ActualizadoTipo = "ClienteActualizado";
    public const string ActualizadoRoutingKey = "cliente.actualizado";

    public const string DesactivadoTipo = "ClienteDesactivado";
    public const string DesactivadoRoutingKey = "cliente.desactivado";
}


public sealed record ClienteEventData
{
    public required string ClienteId { get; init; }
    public required string Nombre { get; init; }
    public required string Identificacion { get; init; }
    public required bool Estado { get; init; }
}
