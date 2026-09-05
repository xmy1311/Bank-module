using Devsu.Account.Application.Interfaces;
using Devsu.Account.Application.Common;
using Devsu.Account.Application.Cuentas.Dtos;
using Devsu.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Account.Infrastructure.Persistence.Repositories;

public sealed class CuentaRepository : ICuentaRepository
{
    private readonly AccountDbContext _context;

    public CuentaRepository(AccountDbContext context) => _context = context;

    public Task<Cuenta?> ObtenerPorNumeroAsync(string numeroCuenta, CancellationToken ct)
        => _context.Cuentas.AsNoTracking().FirstOrDefaultAsync(c => c.NumeroCuenta == numeroCuenta, ct);


    public Task<Cuenta?> ObtenerParaActualizarAsync(string numeroCuenta, CancellationToken ct)
        => _context.Cuentas.FirstOrDefaultAsync(c => c.NumeroCuenta == numeroCuenta, ct);

    public Task<bool> ExisteNumeroCuentaAsync(string numeroCuenta, CancellationToken ct)
        => _context.Cuentas.AsNoTracking().AnyAsync(c => c.NumeroCuenta == numeroCuenta, ct);

    public async Task<PagedResult<Cuenta>> ListarAsync(CuentaQuery query, CancellationToken ct)
    {
        var consulta = _context.Cuentas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.ClienteId))
        {
            var clienteId = query.ClienteId;
            consulta = consulta.Where(c => c.ClienteId == clienteId);
        }

        if (query.TipoCuenta.HasValue)
        {
            consulta = consulta.Where(c => c.TipoCuenta == query.TipoCuenta.Value);
        }

        if (query.Estado.HasValue)
        {
            consulta = consulta.Where(c => c.Estado == query.Estado.Value);
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(c => c.NumeroCuenta)
            .Skip((query.PaginaSegura - 1) * query.TamanoSeguro)
            .Take(query.TamanoSeguro)
            .ToListAsync(ct);

        return new PagedResult<Cuenta>(items, query.PaginaSegura, query.TamanoSeguro, total);
    }

    public void Agregar(Cuenta cuenta) => _context.Cuentas.Add(cuenta);

    public async Task<IReadOnlyCollection<Cuenta>> ObtenerPorClienteParaActualizarAsync(
        string clienteId,
        CancellationToken ct)
        => await _context.Cuentas.Where(c => c.ClienteId == clienteId).ToListAsync(ct);
}

public sealed class ClienteReplicaRepository : IClienteReplicaRepository
{
    private readonly AccountDbContext _context;

    public ClienteReplicaRepository(AccountDbContext context) => _context = context;

    public Task<ClienteReplica?> ObtenerPorClienteIdAsync(string clienteId, CancellationToken ct)
        => _context.ClientesReplica.FirstOrDefaultAsync(c => c.ClienteId == clienteId, ct);

    public void Agregar(ClienteReplica cliente) => _context.ClientesReplica.Add(cliente);
}

public sealed class EventoProcesadoRepository : IEventoProcesadoRepository
{
    private readonly AccountDbContext _context;

    public EventoProcesadoRepository(AccountDbContext context) => _context = context;

    public Task<bool> YaProcesadoAsync(Guid eventId, CancellationToken ct)
        => _context.EventosProcesados.AsNoTracking().AnyAsync(e => e.EventId == eventId, ct);

    public void Registrar(EventoProcesado evento) => _context.EventosProcesados.Add(evento);
}
