using Devsu.Customer.Application.Interfaces;
using Devsu.Customer.Application.Clientes.Dtos;
using Devsu.Customer.Application.Common;
using Devsu.Customer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Customer.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly CustomerDbContext _context;

    public ClienteRepository(CustomerDbContext context) => _context = context;

    /// <summary>Con tracking activo: el resultado se va a modificar.</summary>
    public Task<Cliente?> ObtenerPorClienteIdAsync(string clienteId, CancellationToken ct)
        => _context.Clientes.FirstOrDefaultAsync(c => c.ClienteId == clienteId, ct);

    public Task<bool> ExisteIdentificacionAsync(string identificacion, CancellationToken ct)
        => _context.Personas.AsNoTracking().AnyAsync(p => p.Identificacion == identificacion, ct);

    public Task<bool> ExisteClienteIdAsync(string clienteId, CancellationToken ct)
        => _context.Clientes.AsNoTracking().AnyAsync(c => c.ClienteId == clienteId, ct);

    public async Task<PagedResult<Cliente>> ListarAsync(ClienteQuery query, CancellationToken ct)
    {
        var consulta = _context.Clientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Nombre))
        {
            var nombre = query.Nombre;
            consulta = consulta.Where(c => c.Nombre.Contains(nombre));
        }

        if (!string.IsNullOrWhiteSpace(query.Identificacion))
        {
            var identificacion = query.Identificacion;
            consulta = consulta.Where(c => c.Identificacion == identificacion);
        }

        if (query.Estado.HasValue)
        {
            consulta = consulta.Where(c => c.Estado == query.Estado.Value);
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(c => c.ClienteId)
            .Skip((query.PaginaSegura - 1) * query.TamanoSeguro)
            .Take(query.TamanoSeguro)
            .ToListAsync(ct);

        return new PagedResult<Cliente>(items, query.PaginaSegura, query.TamanoSeguro, total);
    }

    public void Agregar(Cliente cliente) => _context.Clientes.Add(cliente);
}
