using Devsu.Customer.Application.Interfaces;
using Devsu.Customer.Application.Clientes.Dtos;
using Devsu.Customer.Application.Clientes.Mapping;
using Devsu.Customer.Application.Common;
using Devsu.Customer.Domain.Entities;
using Devsu.Customer.Domain.Exceptions;
using Devsu.Customer.Domain.Services;
using Devsu.Shared.Contracts;
using Devsu.Shared.Contracts.Clientes;
using Microsoft.Extensions.Logging;

namespace Devsu.Customer.Application.Clientes;

/// <summary>Orquesta los casos de uso. Las reglas viven en el agregado <see cref="Cliente"/>.</summary>
public sealed class ClienteService : IClienteService
{
    private readonly IClienteRepository _repositorio;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly IClienteIdGenerator _generador;
    private readonly IEventPublisher _publicador;
    private readonly CorrelationContext _correlation;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(
        IClienteRepository repositorio,
        IUnitOfWork unitOfWork,
        IPasswordHasher hasher,
        IClienteIdGenerator generador,
        IEventPublisher publicador,
        CorrelationContext correlation,
        ILogger<ClienteService> logger)
    {
        _repositorio = repositorio;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _generador = generador;
        _publicador = publicador;
        _correlation = correlation;
        _logger = logger;
    }

    public async Task<ClienteResponse> CrearAsync(CrearClienteRequest request, CancellationToken ct)
    {
        if (await _repositorio.ExisteIdentificacionAsync(request.Identificacion, ct))
        {
            throw new ClienteDuplicadoException(request.Identificacion);
        }

        var clienteId = await _generador.SiguienteAsync(ct);

        // El constructor valida y hashea: no puede nacer un Cliente inválido.
        // El '!' es seguro: [Required] sobre un tipo anulable garantiza que
        // ModelState rechazó la petición con 400 si el campo faltaba.
        var cliente = new Cliente(
            clienteId,
            request.Nombre,
            request.Genero!.Value,
            request.Edad!.Value,
            request.Identificacion,
            request.Direccion,
            request.Telefono,
            request.Contrasena,
            _hasher);

        _repositorio.Agregar(cliente);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Cliente {ClienteId} creado.", cliente.ClienteId);

        await PublicarAsync(cliente, EventosCliente.CreadoTipo, EventosCliente.CreadoRoutingKey, ct);

        return cliente.ToResponse();
    }

    public async Task<ClienteResponse> ObtenerAsync(string clienteId, CancellationToken ct)
    {
        var cliente = await ObtenerOFallarAsync(clienteId, ct);
        return cliente.ToResponse();
    }

    public async Task<PagedResult<ClienteResponse>> ListarAsync(ClienteQuery query, CancellationToken ct)
    {
        var pagina = await _repositorio.ListarAsync(query, ct);

        return new PagedResult<ClienteResponse>(
            pagina.Items.Select(c => c.ToResponse()).ToList(),
            pagina.Pagina,
            pagina.TamanoPagina,
            pagina.TotalRegistros);
    }

    public async Task<ClienteResponse> ActualizarAsync(
        string clienteId,
        ActualizarClienteRequest request,
        CancellationToken ct)
    {
        var cliente = await ObtenerOFallarAsync(clienteId, ct);

        cliente.ActualizarDatosPersonales(
            request.Nombre,
            request.Genero!.Value,
            request.Edad!.Value,
            request.Direccion,
            request.Telefono);

        AplicarEstado(cliente, request.Estado!.Value);

        await _unitOfWork.SaveChangesAsync(ct);
        await PublicarAsync(cliente, EventosCliente.ActualizadoTipo, EventosCliente.ActualizadoRoutingKey, ct);

        return cliente.ToResponse();
    }

    public async Task<ClienteResponse> ActualizarParcialAsync(
        string clienteId,
        ActualizarParcialClienteRequest request,
        CancellationToken ct)
    {
        var cliente = await ObtenerOFallarAsync(clienteId, ct);

        cliente.ActualizarDatosPersonales(
            request.Nombre ?? cliente.Nombre,
            request.Genero ?? cliente.Genero,
            request.Edad ?? cliente.Edad,
            request.Direccion ?? cliente.Direccion,
            request.Telefono ?? cliente.Telefono);

        if (request.Estado.HasValue)
        {
            AplicarEstado(cliente, request.Estado.Value);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await PublicarAsync(cliente, EventosCliente.ActualizadoTipo, EventosCliente.ActualizadoRoutingKey, ct);

        return cliente.ToResponse();
    }

    /// <summary>
    /// DELETE = baja lógica (A8 / RN-07). El evento permite que el Account Service
    /// inactive las cuentas del cliente. Los datos financieros se conservan.
    /// </summary>
    public async Task DesactivarAsync(string clienteId, CancellationToken ct)
    {
        var cliente = await ObtenerOFallarAsync(clienteId, ct);

        cliente.Desactivar();
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Cliente {ClienteId} desactivado (baja lógica).", clienteId);

        await PublicarAsync(cliente, EventosCliente.DesactivadoTipo, EventosCliente.DesactivadoRoutingKey, ct);
    }

    public async Task CambiarPasswordAsync(string clienteId, CambiarPasswordRequest request, CancellationToken ct)
    {
        var cliente = await ObtenerOFallarAsync(clienteId, ct);

        if (!cliente.VerificarPassword(request.ContrasenaActual, _hasher))
        {
            throw new DomainException("CREDENCIALES_INVALIDAS", "La contraseña actual no es correcta.");
        }

        cliente.EstablecerPassword(request.ContrasenaNueva, _hasher);
        await _unitOfWork.SaveChangesAsync(ct);

        // Sin datos de la contraseña en el log, ni siquiera su longitud.
        _logger.LogInformation("Contraseña actualizada para el cliente {ClienteId}.", clienteId);
    }

    private async Task<Cliente> ObtenerOFallarAsync(string clienteId, CancellationToken ct)
        => await _repositorio.ObtenerPorClienteIdAsync(clienteId, ct)
           ?? throw new EntidadNoEncontradaException("el cliente", clienteId);

    private static void AplicarEstado(Cliente cliente, bool estado)
    {
        if (estado)
        {
            cliente.Reactivar();
        }
        else
        {
            cliente.Desactivar();
        }
    }

    /// <summary>
    /// LIMITACIÓN CONOCIDA: el commit y esta publicación no son atómicos. Si el
    /// proceso muere entre ambos, la réplica queda desactualizada. Lo resolvería el
    /// Outbox Pattern; ver DECISIONS.md (D14). Mitigación: log de error explícito y
    /// EventId estable para reproceso manual.
    /// </summary>
    private async Task PublicarAsync(Cliente cliente, string tipo, string routingKey, CancellationToken ct)
    {
        var evento = new IntegrationEvent<ClienteEventData>
        {
            EventType = tipo,
            CorrelationId = _correlation.CorrelationId,
            Data = new ClienteEventData
            {
                ClienteId = cliente.ClienteId,
                Nombre = cliente.Nombre,
                Identificacion = cliente.Identificacion,
                Estado = cliente.Estado
            }
        };

        try
        {
            await _publicador.PublishAsync(evento, routingKey, ct);
        }
        catch (Exception ex)
        {
            // No se propaga: el cliente YA está persistido y la operación HTTP fue
            // correcta. Fallar aquí daría un 500 tras un commit exitoso.
            _logger.LogError(
                ex,
                "No se pudo publicar {EventType} para {ClienteId}. EventId={EventId}. " +
                "La réplica del Account Service quedará desactualizada hasta un reproceso.",
                tipo,
                cliente.ClienteId,
                evento.EventId);
        }
    }
}
