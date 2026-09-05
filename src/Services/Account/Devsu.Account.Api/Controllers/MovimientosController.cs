using Devsu.Account.Application.Common;
using Devsu.Account.Application.Movimientos;
using Devsu.Account.Application.Movimientos.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Account.Api.Controllers;

/// <summary>
/// F2 y F3.
///
/// NO existe Update (decisión A17): un asiento contable es inmutable. Modificar
/// el valor de una transacción registrada rompería la ecuación
/// SaldoInicial + SUM(Movimientos) = SaldoDisponible, invalidaría el histórico y
/// eliminaría la trazabilidad que exige F2. La operación correcta para corregir
/// un movimiento erróneo es el REVERSO: un contra-asiento de igual valor y signo
/// opuesto, quedando ambos registros visibles en el estado de cuenta.
/// </summary>
[ApiController]
[Route("api/movimientos")]
[Produces("application/json")]
public sealed class MovimientosController : ControllerBase
{
    private readonly IMovimientoService _servicio;

    public MovimientosController(IMovimientoService servicio) => _servicio = servicio;

    [HttpGet]
    [ProducesResponseType<PagedResult<MovimientoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MovimientoResponse>>> Listar(
        [FromQuery] MovimientoQuery query,
        CancellationToken ct)
        => Ok(await _servicio.ListarAsync(query, ct));

    [HttpGet("{movimientoId:guid}")]
    [ProducesResponseType<MovimientoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoResponse>> Obtener(Guid movimientoId, CancellationToken ct)
        => Ok(await _servicio.ObtenerAsync(movimientoId, ct));

    /// <summary>
    /// Registra un movimiento. El valor lleva SIGNO: positivo depósito, negativo
    /// retiro. El tipo lo deriva el dominio, así que no puede contradecir al valor.
    ///
    /// Si el saldo resultante quedara negativo responde 422 con el mensaje
    /// "Saldo no disponible" y el código SALDO_NO_DISPONIBLE (F3).
    /// </summary>
    [HttpPost]
    [ProducesResponseType<MovimientoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MovimientoResponse>> Registrar(
        [FromBody] CrearMovimientoRequest request,
        CancellationToken ct)
    {
        var movimiento = await _servicio.RegistrarAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { movimientoId = movimiento.MovimientoId }, movimiento);
    }
}
