using Devsu.Account.Application.Common;
using Devsu.Account.Application.Cuentas.Mapping;
using Devsu.Account.Application.Exceptions;
using Devsu.Account.Application.Interfaces;
using Devsu.Account.Application.Movimientos.Dtos;
using Devsu.Account.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Devsu.Account.Application.Movimientos;

public interface IMovimientoService
{
    Task<MovimientoResponse> RegistrarAsync(CrearMovimientoRequest request, CancellationToken ct);

    Task<MovimientoResponse> ObtenerAsync(Guid movimientoId, CancellationToken ct);

    Task<PagedResult<MovimientoResponse>> ListarAsync(MovimientoQuery query, CancellationToken ct);
}


public sealed class MovimientoService : IMovimientoService
{

    /// <summary>
    /// maximo número de intentos ante un conflicto de concurrencia. El flujo crítico
    /// </summary>
    private const int MaxIntentos = 3;

    private readonly ICuentaRepository _cuentas;
    private readonly IMovimientoQueryService _consultas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MovimientoService> _logger;

    public MovimientoService(
        ICuentaRepository cuentas,
        IMovimientoQueryService consultas,
        IUnitOfWork unitOfWork,
        ILogger<MovimientoService> logger)
    {
        _cuentas = cuentas;
        _consultas = consultas;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MovimientoResponse> RegistrarAsync(CrearMovimientoRequest request, CancellationToken ct)
    {
        var fecha = request.Fecha ?? DateTime.UtcNow;

        for (var intento = 1; ; intento++)
        {
            var cuenta = await _cuentas.ObtenerParaActualizarAsync(request.NumeroCuenta, ct)
                ?? throw new EntidadNoEncontradaException("la cuenta", request.NumeroCuenta);

            var movimiento = cuenta.RegistrarMovimiento(request.Valor!.Value, fecha);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Movimiento {Tipo} registrado en la cuenta {NumeroCuenta}. Saldo resultante: {Saldo}.",
                    movimiento.TipoMovimiento,
                    cuenta.NumeroCuenta,
                    movimiento.Saldo);

                return movimiento.ToResponse(cuenta.NumeroCuenta);
            }
            catch (ConflictoConcurrenciaException) when (intento < MaxIntentos)
            {
                _logger.LogWarning(
                    "Conflicto de concurrencia en la cuenta {NumeroCuenta}. Reintento {Intento} de {Max}.",
                    request.NumeroCuenta,
                    intento,
                    MaxIntentos);

                // Estado en memoria descartado: el siguiente intento relee el saldo real.
                _unitOfWork.DescartarCambios();
            }
        }
    }

    public async Task<MovimientoResponse> ObtenerAsync(Guid movimientoId, CancellationToken ct)
        => await _consultas.ObtenerPorIdAsync(movimientoId, ct)
           ?? throw new EntidadNoEncontradaException("el movimiento", movimientoId.ToString());

    public Task<PagedResult<MovimientoResponse>> ListarAsync(MovimientoQuery query, CancellationToken ct)
        => _consultas.ListarAsync(query, ct);
}
