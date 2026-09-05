using Devsu.Customer.Application.Clientes.Dtos;
using Devsu.Customer.Application.Common;
using Devsu.Customer.Domain.Entities;

namespace Devsu.Customer.Application.Interfaces;


public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorClienteIdAsync(string clienteId, CancellationToken ct);

    Task<bool> ExisteIdentificacionAsync(string identificacion, CancellationToken ct);

    Task<bool> ExisteClienteIdAsync(string clienteId, CancellationToken ct);

    Task<PagedResult<Cliente>> ListarAsync(ClienteQuery query, CancellationToken ct);

    void Agregar(Cliente cliente);
}
