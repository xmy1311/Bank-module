using System.Globalization;
using Devsu.Account.Application.Interfaces;
using Devsu.Account.Application.Reportes.Dtos;
using Devsu.Account.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Account.Api.Controllers;

/// <summary>
/// Estado de cuenta por cliente y rango de fechas.
/// Url: /api/reportes?fecha=2022-02-01,2022-02-28&amp;cliente=CLI-0002
/// </summary>
[ApiController]
[Route("api/reportes")]
[Produces("application/json")]
public sealed class ReportesController : ControllerBase
{
    private readonly IReporteQueryService _reportes;

    public ReportesController(IReporteQueryService reportes) => _reportes = reportes;

    [HttpGet]
    [ProducesResponseType<ReporteEstadoCuentaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReporteEstadoCuentaResponse>> Generar(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? clienteId,
        [FromQuery] string? fecha,
        [FromQuery] string? cliente,
        CancellationToken ct)
    {
        var id = clienteId ?? cliente;

        if (string.IsNullOrWhiteSpace(id))
        {
            ModelState.AddModelError(
                "clienteId",
                "Es obligatorio indicar el cliente (clienteId, o su alias cliente).");

            return ValidationProblem(ModelState);
        }

        // Alias del enunciado: fecha=inicio,fin  (también acepta 'inicio a fin')
        if (!fechaInicio.HasValue && !fechaFin.HasValue && !string.IsNullOrWhiteSpace(fecha))
        {
            var partes = fecha.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (partes.Length != 2
                || !DateTime.TryParse(partes[0], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var desde)
                || !DateTime.TryParse(partes[1], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var hasta))
            {
                ModelState.AddModelError(
                    "fecha",
                    "El rango debe tener el formato 'yyyy-MM-dd,yyyy-MM-dd'. " +
                    "También puedes usar los parámetros fechaInicio y fechaFin.");

                return ValidationProblem(ModelState);
            }

            fechaInicio = desde;
            fechaFin = hasta;
        }

        if (!fechaInicio.HasValue || !fechaFin.HasValue)
        {
            ModelState.AddModelError(
                "fechaInicio",
                "Es obligatorio indicar el rango: fechaInicio y fechaFin, o el alias fecha=inicio,fin.");

            return ValidationProblem(ModelState);
        }

        if (fechaInicio > fechaFin)
        {
            ModelState.AddModelError("fechaInicio", "La fecha inicial no puede ser posterior a la final.");

            return ValidationProblem(ModelState);
        }

        // Normalización a UTC.
        var inicio = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);

        // El rango es INCLUSIVO
        var fin = DateTime.SpecifyKind(
            fechaFin.Value.TimeOfDay == TimeSpan.Zero
                ? fechaFin.Value.Date.AddDays(1).AddTicks(-1)
                : fechaFin.Value,
            DateTimeKind.Utc);

        var reporte = await _reportes.GenerarAsync(id, inicio, fin, ct)
            ?? throw new EntidadNoEncontradaException("el cliente", id);

        return Ok(reporte);
    }
}
