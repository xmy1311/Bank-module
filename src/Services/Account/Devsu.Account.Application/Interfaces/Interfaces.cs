using Devsu.Account.Application.Common;
using Devsu.Account.Application.Cuentas.Dtos;
using Devsu.Account.Application.Movimientos.Dtos;
using Devsu.Account.Application.Reportes.Dtos;
using Devsu.Account.Domain.Entities;

namespace Devsu.Account.Application.Interfaces;

public interface ICuentaRepository
{

    Task<Cuenta?> ObtenerPorNumeroAsync(string numeroCuenta, CancellationToken ct);

     Task<Cuenta?> ObtenerParaActualizarAsync(string numeroCuenta, CancellationToken ct);

    Task<bool> ExisteNumeroCuentaAsync(string numeroCuenta, CancellationToken ct);

    Task<PagedResult<Cuenta>> ListarAsync(CuentaQuery query, CancellationToken ct);

    void Agregar(Cuenta cuenta);

    Task<IReadOnlyCollection<Cuenta>> ObtenerPorClienteParaActualizarAsync(string clienteId, CancellationToken ct);
}

public interface IClienteReplicaRepository
{
    Task<ClienteReplica?> ObtenerPorClienteIdAsync(string clienteId, CancellationToken ct);

    void Agregar(ClienteReplica cliente);
}

public interface IEventoProcesadoRepository
{
    Task<bool> YaProcesadoAsync(Guid eventId, CancellationToken ct);

    void Registrar(EventoProcesado evento);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);

  
    void DescartarCambios();
}


public interface IMovimientoQueryService
{
    Task<PagedResult<MovimientoResponse>> ListarAsync(MovimientoQuery query, CancellationToken ct);

    Task<MovimientoResponse?> ObtenerPorIdAsync(Guid movimientoId, CancellationToken ct);
}

public interface IReporteQueryService
{
    Task<ReporteEstadoCuentaResponse?> GenerarAsync(
        string clienteId,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken ct);
}
