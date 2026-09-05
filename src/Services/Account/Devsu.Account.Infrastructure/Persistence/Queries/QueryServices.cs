using Devsu.Account.Application.Interfaces;
using Devsu.Account.Application.Common;
using Devsu.Account.Application.Movimientos.Dtos;
using Devsu.Account.Application.Reportes.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Account.Infrastructure.Persistence.Queries;

/// <summary>
/// Optimización de lectura: Carga los datos directamente en un DTO sin rastreo (AsNoTracking).
/// Evita cargar la entidad Cuenta con miles de movimientos en memoria, previniendo problemas de rendimiento.
/// </summary>
public sealed class MovimientoQueryService : IMovimientoQueryService
{
    private readonly AccountDbContext _context;

    public MovimientoQueryService(AccountDbContext context) => _context = context;

    public async Task<PagedResult<MovimientoResponse>> ListarAsync(MovimientoQuery query, CancellationToken ct)
    {
        // filtrar por número de cuenta y por cliente
        var consulta =
            from m in _context.Movimientos.AsNoTracking()
            join c in _context.Cuentas.AsNoTracking() on m.CuentaId equals c.CuentaId
            select new { m, c };

        if (!string.IsNullOrWhiteSpace(query.NumeroCuenta))
        {
            var numero = query.NumeroCuenta;
            consulta = consulta.Where(x => x.c.NumeroCuenta == numero);
        }

        if (!string.IsNullOrWhiteSpace(query.ClienteId))
        {
            var clienteId = query.ClienteId;
            consulta = consulta.Where(x => x.c.ClienteId == clienteId);
        }

        if (query.TipoMovimiento.HasValue)
        {
            consulta = consulta.Where(x => x.m.TipoMovimiento == query.TipoMovimiento.Value);
        }

        if (query.FechaInicio.HasValue)
        {
            var desde = query.FechaInicio.Value;
            consulta = consulta.Where(x => x.m.Fecha >= desde);
        }

        if (query.FechaFin.HasValue)
        {
            var hasta = query.FechaFin.Value;
            consulta = consulta.Where(x => x.m.Fecha <= hasta);
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(x => x.m.Fecha)
            .ThenByDescending(x => x.m.RegistradoEn)
            .Skip((query.PaginaSegura - 1) * query.TamanoSeguro)
            .Take(query.TamanoSeguro)
            .Select(x => new MovimientoResponse
            {
                MovimientoId = x.m.MovimientoId,
                NumeroCuenta = x.c.NumeroCuenta,
                Fecha = x.m.Fecha,
                TipoMovimiento = x.m.TipoMovimiento.ToString(),
                Valor = x.m.Valor,
                Saldo = x.m.Saldo,
                RegistradoEn = x.m.RegistradoEn
            })
            .ToListAsync(ct);

        return new PagedResult<MovimientoResponse>(items, query.PaginaSegura, query.TamanoSeguro, total);
    }

    public Task<MovimientoResponse?> ObtenerPorIdAsync(Guid movimientoId, CancellationToken ct)
        => (from m in _context.Movimientos.AsNoTracking()
            join c in _context.Cuentas.AsNoTracking() on m.CuentaId equals c.CuentaId
            where m.MovimientoId == movimientoId
            select new MovimientoResponse
            {
                MovimientoId = m.MovimientoId,
                NumeroCuenta = c.NumeroCuenta,
                Fecha = m.Fecha,
                TipoMovimiento = m.TipoMovimiento.ToString(),
                Valor = m.Valor,
                Saldo = m.Saldo,
                RegistradoEn = m.RegistradoEn
            }).FirstOrDefaultAsync(ct);
}


public sealed class ReporteQueryService : IReporteQueryService
{
    private readonly AccountDbContext _context;

    public ReporteQueryService(AccountDbContext context) => _context = context;

    public async Task<ReporteEstadoCuentaResponse?> GenerarAsync(
        string clienteId,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken ct)
    {
        var cliente = await _context.ClientesReplica
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .Select(c => new ReporteClienteDto
            {
                ClienteId = c.ClienteId,
                Nombre = c.Nombre,
                Identificacion = c.Identificacion,
                Estado = c.Estado
            })
            .FirstOrDefaultAsync(ct);

        if (cliente is null)
        {
            return null;
        }

        var cuentas = await _context.Cuentas
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .OrderBy(c => c.NumeroCuenta)
            .Select(c => new ReporteCuentaDto
            {
                NumeroCuenta = c.NumeroCuenta,
                TipoCuenta = c.TipoCuenta.ToString(),
                SaldoInicial = c.SaldoInicial,
                SaldoDisponible = c.SaldoDisponible,
                Estado = c.Estado,
                Movimientos = c.Movimientos
                    .Where(m => m.Fecha >= fechaInicio && m.Fecha <= fechaFin)
                    .OrderByDescending(m => m.Fecha)
                    .Select(m => new ReporteMovimientoDto
                    {
                        Fecha = m.Fecha,
                        TipoMovimiento = m.TipoMovimiento.ToString(),
                        Valor = m.Valor,
                        Saldo = m.Saldo
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        return new ReporteEstadoCuentaResponse
        {
            Cliente = cliente,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Cuentas = cuentas
        };
    }
}
