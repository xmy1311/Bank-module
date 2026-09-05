using Devsu.Customer.Application.Clientes.Dtos;
using Devsu.Customer.Application.Common;

namespace Devsu.Customer.Application.Clientes;

public interface IClienteService
{
    Task<ClienteResponse> CrearAsync(CrearClienteRequest request, CancellationToken ct);

    Task<ClienteResponse> ObtenerAsync(string clienteId, CancellationToken ct);

    Task<PagedResult<ClienteResponse>> ListarAsync(ClienteQuery query, CancellationToken ct);

    Task<ClienteResponse> ActualizarAsync(string clienteId, ActualizarClienteRequest request, CancellationToken ct);

    Task<ClienteResponse> ActualizarParcialAsync(string clienteId, ActualizarParcialClienteRequest request, CancellationToken ct);

    Task DesactivarAsync(string clienteId, CancellationToken ct);

    Task CambiarPasswordAsync(string clienteId, CambiarPasswordRequest request, CancellationToken ct);
}
