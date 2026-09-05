namespace Devsu.Account.Domain.Entities;

/// <summary>
/// Registro de idempotencia del consumidor de eventos
/// La inserción de esta fila y la actualización de la réplica ocurren
/// en la MISMA transacción: o pasan las dos, o ninguna.
/// </summary>
public class EventoProcesado
{
    public Guid EventId { get; private set; }
    public string TipoEvento { get; private set; } = null!;
    public DateTime ProcesadoEn { get; private set; }

    private EventoProcesado()
    {
    }

    public EventoProcesado(Guid eventId, string tipoEvento)
    {
        EventId = eventId;
        TipoEvento = tipoEvento;
        ProcesadoEn = DateTime.UtcNow;
    }
}
