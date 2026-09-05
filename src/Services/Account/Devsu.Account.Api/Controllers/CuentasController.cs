using Devsu.Account.Application.Common;
using Devsu.Account.Application.Cuentas;
using Devsu.Account.Application.Cuentas.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Account.Api.Controllers;

/// <summary>CRU de Cuenta (F1)</summary>
[ApiController]
[Route("api/cuentas")]
[Produces("application/json")]
public sealed class CuentasController : ControllerBase
{
    private readonly ICuentaService _servicio;

    public CuentasController(ICuentaService servicio) => _servicio = servicio;

    [HttpGet]
    [ProducesResponseType<PagedResult<CuentaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CuentaResponse>>> Listar(
        [FromQuery] CuentaQuery query,
        CancellationToken ct)
        => Ok(await _servicio.ListarAsync(query, ct));

    [HttpGet("{numeroCuenta}")]
    [ProducesResponseType<CuentaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaResponse>> Obtener(string numeroCuenta, CancellationToken ct)
        => Ok(await _servicio.ObtenerAsync(numeroCuenta, ct));

    /// <summary>
    /// Valida al cliente localmente sin invocar al Customer Service.
    /// Retorna 404 si los datos no se han replicado aún (comportamiento de consistencia eventual por diseño).
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CuentaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CuentaResponse>> Crear(
        [FromBody] CrearCuentaRequest request,
        CancellationToken ct)
    {
        var creada = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { numeroCuenta = creada.NumeroCuenta }, creada);
    }

    /// <summary>
    /// Solo tipo y estado son editables. SaldoInicial no se edita
    /// </summary>
    [HttpPut("{numeroCuenta}")]
    [ProducesResponseType<CuentaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaResponse>> Actualizar(
        string numeroCuenta,
        [FromBody] ActualizarCuentaRequest request,
        CancellationToken ct)
        => Ok(await _servicio.ActualizarAsync(numeroCuenta, request, ct));


    [HttpPatch("{numeroCuenta}")]
    [ProducesResponseType<CuentaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaResponse>> ActualizarParcial(
        string numeroCuenta,
        [FromBody] ActualizarParcialCuentaRequest request,
        CancellationToken ct)
        => Ok(await _servicio.ActualizarParcialAsync(numeroCuenta, request, ct));
}
