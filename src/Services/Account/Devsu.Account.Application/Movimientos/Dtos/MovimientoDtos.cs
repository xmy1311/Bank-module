using System.ComponentModel.DataAnnotations;
using Devsu.Account.Domain.Enums;

namespace Devsu.Account.Application.Movimientos.Dtos;

/// <summary>
/// El cliente envía SOLO el valor CON SIGNO (+,-)
///
/// El TipoMovimiento se infiere del signo: positivo = depósito, negativo = retiro.
/// </summary>
public sealed record CrearMovimientoRequest
{
    [Required, StringLength(20, MinimumLength = 4)]
    public string NumeroCuenta { get; init; } = null!;

    // Anulable + [Required]: campo obligatorio de tipo valor.
    [Required]
    public decimal? Valor { get; init; }

    /// fecha y hora actual (UTC) por defecto</summary>
    public DateTime? Fecha { get; init; }
}

public sealed record MovimientoResponse
{
    public required Guid MovimientoId { get; init; }
    public required string NumeroCuenta { get; init; }
    public required DateTime Fecha { get; init; }
    public required string TipoMovimiento { get; init; }
    public required decimal Valor { get; init; }
    public required decimal Saldo { get; init; }
    public required DateTime RegistradoEn { get; init; }
}

public sealed record MovimientoQuery
{
    private const int TamanoMaximo = 100;

    public string? NumeroCuenta { get; init; }
    public string? ClienteId { get; init; }
    public TipoMovimiento? TipoMovimiento { get; init; }
    public DateTime? FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, TamanoMaximo)]
    public int TamanoPagina { get; init; } = 20;

    public int PaginaSegura => Pagina < 1 ? 1 : Pagina;

    public int TamanoSeguro => TamanoPagina is < 1 or > TamanoMaximo ? 20 : TamanoPagina;
}
