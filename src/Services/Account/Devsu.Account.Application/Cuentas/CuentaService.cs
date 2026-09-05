using Devsu.Account.Application.Interfaces;
using Devsu.Account.Application.Common;
using Devsu.Account.Application.Cuentas.Dtos;
using Devsu.Account.Application.Cuentas.Mapping;
using Devsu.Account.Domain.Entities;
using Devsu.Account.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Devsu.Account.Application.Cuentas;

public interface ICuentaService
{
    Task<CuentaResponse> CrearAsync(CrearCuentaRequest request, CancellationToken ct);

    Task<CuentaResponse> ObtenerAsync(string numeroCuenta, CancellationToken ct);

    Task<PagedResult<CuentaResponse>> ListarAsync(CuentaQuery query, CancellationToken ct);

    Task<CuentaResponse> ActualizarAsync(string numeroCuenta, ActualizarCuentaRequest request, CancellationToken ct);

    Task<CuentaResponse> ActualizarParcialAsync(string numeroCuenta, ActualizarParcialCuentaRequest request, CancellationToken ct);
}

public sealed class CuentaService : ICuentaService
{
    private readonly ICuentaRepository _cuentas;
    private readonly IClienteReplicaRepository _clientes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CuentaService> _logger;

    public CuentaService(
        ICuentaRepository cuentas,
        IClienteReplicaRepository clientes,
        IUnitOfWork unitOfWork,
        ILogger<CuentaService> logger)
    {
        _cuentas = cuentas;
        _clientes = clientes;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Valida al cliente contra la réplica local para permitir la creación de cuentas
    /// aun si el Customer Service está caído.
    /// Retorna 404 si el evento de sincronización no ha llegado (consistencia eventual por diseño).
    /// </summary>

    public async Task<CuentaResponse> CrearAsync(CrearCuentaRequest request, CancellationToken ct)
    {
        var cliente = await _clientes.ObtenerPorClienteIdAsync(request.ClienteId, ct)
            ?? throw new EntidadNoEncontradaException("el cliente", request.ClienteId);

        if (!cliente.Estado)
        {
            throw new ClienteInactivoException(request.ClienteId);
        }

        if (await _cuentas.ExisteNumeroCuentaAsync(request.NumeroCuenta, ct))
        {
            throw new CuentaDuplicadaException(request.NumeroCuenta);
        }

        // El '!' garantiza que el valor no es nulo
        var cuenta = new Cuenta(
            request.NumeroCuenta,
            request.TipoCuenta!.Value,
            request.SaldoInicial!.Value,
            request.ClienteId);

        _cuentas.Agregar(cuenta);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Cuenta {NumeroCuenta} creada para el cliente {ClienteId}.",
            cuenta.NumeroCuenta,
            cuenta.ClienteId);

        return cuenta.ToResponse();
    }

    public async Task<CuentaResponse> ObtenerAsync(string numeroCuenta, CancellationToken ct)
        => (await ObtenerOFallarAsync(numeroCuenta, ct)).ToResponse();

    public async Task<PagedResult<CuentaResponse>> ListarAsync(CuentaQuery query, CancellationToken ct)
    {
        var pagina = await _cuentas.ListarAsync(query, ct);

        return new PagedResult<CuentaResponse>(
            pagina.Items.Select(c => c.ToResponse()).ToList(),
            pagina.Pagina,
            pagina.TamanoPagina,
            pagina.TotalRegistros);
    }

    public async Task<CuentaResponse> ActualizarAsync(
        string numeroCuenta,
        ActualizarCuentaRequest request,
        CancellationToken ct)
    {
        var cuenta = await ObtenerParaActualizarOFallarAsync(numeroCuenta, ct);

        cuenta.CambiarTipo(request.TipoCuenta!.Value);
        AplicarEstado(cuenta, request.Estado!.Value);

        await _unitOfWork.SaveChangesAsync(ct);

        return cuenta.ToResponse();
    }

    public async Task<CuentaResponse> ActualizarParcialAsync(
        string numeroCuenta,
        ActualizarParcialCuentaRequest request,
        CancellationToken ct)
    {
        var cuenta = await ObtenerParaActualizarOFallarAsync(numeroCuenta, ct);

        if (request.TipoCuenta.HasValue)
        {
            cuenta.CambiarTipo(request.TipoCuenta.Value);
        }

        if (request.Estado.HasValue)
        {
            AplicarEstado(cuenta, request.Estado.Value);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return cuenta.ToResponse();
    }

    private async Task<Cuenta> ObtenerOFallarAsync(string numeroCuenta, CancellationToken ct)
        => await _cuentas.ObtenerPorNumeroAsync(numeroCuenta, ct)
           ?? throw new EntidadNoEncontradaException("la cuenta", numeroCuenta);

    private async Task<Cuenta> ObtenerParaActualizarOFallarAsync(string numeroCuenta, CancellationToken ct)
        => await _cuentas.ObtenerParaActualizarAsync(numeroCuenta, ct)
           ?? throw new EntidadNoEncontradaException("la cuenta", numeroCuenta);

    private static void AplicarEstado(Cuenta cuenta, bool estado)
    {
        if (estado)
        {
            cuenta.Activar();
        }
        else
        {
            cuenta.Desactivar();
        }
    }
}
