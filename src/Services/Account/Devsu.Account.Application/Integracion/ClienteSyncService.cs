using Devsu.Account.Application.Interfaces;
using Devsu.Account.Domain.Entities;
using Devsu.Shared.Contracts;
using Devsu.Shared.Contracts.Clientes;
using Microsoft.Extensions.Logging;

namespace Devsu.Account.Application.Integracion;

public enum ResultadoSincronizacion
{

    Aplicado,

    Duplicado,

    Obsoleto
}

/// <summary>
/// Aplica los eventos de cliente sobre la copia  local del Account Service.
/// </summary>
public interface IClienteSyncService
{
    /// <summary>
    /// Procesa un evento de cliente. Es seguro llamarlo con el mismo evento varias
    /// veces: devuelve <see cref="ResultadoSincronizacion.Duplicado"/> u
    /// <see cref="ResultadoSincronizacion.Obsoleto"/>
    /// </summary>
    Task<ResultadoSincronizacion> AplicarAsync(IntegrationEvent<ClienteEventData> evento, CancellationToken ct);
}

public sealed class ClienteSyncService : IClienteSyncService
{
    private readonly IClienteReplicaRepository _replica;
    private readonly IEventoProcesadoRepository _eventos;
    private readonly ICuentaRepository _cuentas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClienteSyncService> _logger;

    public ClienteSyncService(
        IClienteReplicaRepository replica,
        IEventoProcesadoRepository eventos,
        ICuentaRepository cuentas,
        IUnitOfWork unitOfWork,
        ILogger<ClienteSyncService> logger)
    {
        _replica = replica;
        _eventos = eventos;
        _cuentas = cuentas;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

     public async Task<ResultadoSincronizacion> AplicarAsync(
        IntegrationEvent<ClienteEventData> evento,
        CancellationToken ct)
    {
        if (await _eventos.YaProcesadoAsync(evento.EventId, ct))
        {
            _logger.LogInformation(
                "Evento {EventId} ya procesado anteriormente. Se descarta.", evento.EventId);

            return ResultadoSincronizacion.Duplicado;
        }

        var datos = evento.Data;
        var existente = await _replica.ObtenerPorClienteIdAsync(datos.ClienteId, ct);

        if (existente is null)
        {
            //  upsert.
            _replica.Agregar(new ClienteReplica(
                datos.ClienteId, datos.Nombre, datos.Identificacion, datos.Estado, evento.OccurredOn));
        }
        else if (!existente.Aplicar(datos.Nombre, datos.Identificacion, datos.Estado, evento.OccurredOn))
        {
            
            _logger.LogWarning(
                "Evento {EventType} de {ClienteId} descartado por obsoleto (occurredOn {OccurredOn}).",
                evento.EventType, datos.ClienteId, evento.OccurredOn);

            _eventos.Registrar(new EventoProcesado(evento.EventId, evento.EventType));
            await _unitOfWork.SaveChangesAsync(ct);

            return ResultadoSincronizacion.Obsoleto;
        }

        
        // ClienteActualizado que traiga estado=false también debe inactivar las  cuentas
        // Los MOVIMIENTOS no se cambian: la información financiera permanece.
        if (evento.EventType == EventosCliente.DesactivadoTipo || !datos.Estado)
        {
            var cuentas = await _cuentas.ObtenerPorClienteParaActualizarAsync(datos.ClienteId, ct);

            foreach (var cuenta in cuentas)
            {
                cuenta.Desactivar();
            }

            if (cuentas.Count > 0)
            {
                _logger.LogInformation(
                    "Desactivadas {Cantidad} cuenta(s) del cliente {ClienteId}.",
                    cuentas.Count, datos.ClienteId);
            }
        }

        _eventos.Registrar(new EventoProcesado(evento.EventId, evento.EventType));
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Réplica sincronizada por {EventType}: cliente {ClienteId} (correlationId {CorrelationId}).",
            evento.EventType, datos.ClienteId, evento.CorrelationId);

        return ResultadoSincronizacion.Aplicado;
    }
}
