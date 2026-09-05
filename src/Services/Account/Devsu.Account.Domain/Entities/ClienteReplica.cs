namespace Devsu.Account.Domain.Entities;

/// <summary>
/// Read model de integración, solo aplica los eventos del Customer Service.
/// </summary>
public class ClienteReplica
{
    public string ClienteId { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string Identificacion { get; private set; } = null!;
    public bool Estado { get; private set; }
    public DateTime ActualizadoEn { get; private set; }

    private ClienteReplica()
    {
    }

    public ClienteReplica(string clienteId, string nombre, string identificacion, bool estado, DateTime occurredOn)
    {
        ClienteId = clienteId;
        Nombre = nombre;
        Identificacion = identificacion;
        Estado = estado;
        ActualizadoEn = occurredOn;
    }

    /// <summary>
    /// Aplica un evento; devuelve false si llega obsoleto.
    /// </summary>
    public bool Aplicar(string nombre, string identificacion, bool estado, DateTime occurredOn)
    {
        if (occurredOn <= ActualizadoEn)
        {
            return false;
        }

        Nombre = nombre;
        Identificacion = identificacion;
        Estado = estado;
        ActualizadoEn = occurredOn;

        return true;
    }
}
