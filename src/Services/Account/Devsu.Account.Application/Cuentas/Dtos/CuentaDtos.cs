using System.ComponentModel.DataAnnotations;
using Devsu.Account.Domain.Enums;

namespace Devsu.Account.Application.Cuentas.Dtos;

// Los campos obligatorios de tipo valor van ANULABLES con [Required].
// Sobre un tipo no anulable el atributo no valida nada —un bool nunca es null—,
// así que omitir el campo lo dejaría en su valor por defecto sin dar error.


public sealed record CrearCuentaRequest
{
    [Required, StringLength(20, MinimumLength = 4)]
    public string NumeroCuenta { get; init; } = null!;

    [Required]
    public TipoCuenta? TipoCuenta { get; init; }

    [Required]
    [Range(0, 9_999_999_999_999.99)]
    public decimal? SaldoInicial { get; init; }

    [Required, StringLength(20, MinimumLength = 3)]
    public string ClienteId { get; init; } = null!;
}

/// <summary>
/// PUT: reemplaza lo modificable. SaldoInicial y ClienteId NO son editables —
/// cambiar el saldo de apertura rompería la ecuación del saldo, y mover una cuenta
/// de titular es una operación de negocio distinta, no una actualización.
/// </summary>
public sealed record ActualizarCuentaRequest
{
    [Required]
    public TipoCuenta? TipoCuenta { get; init; }

    [Required]
    public bool? Estado { get; init; }
}

public sealed record ActualizarParcialCuentaRequest
{
    public TipoCuenta? TipoCuenta { get; init; }

    public bool? Estado { get; init; }
}

public sealed record CuentaResponse
{
    public required string NumeroCuenta { get; init; }
    public required string TipoCuenta { get; init; }
    public required decimal SaldoInicial { get; init; }
    public required decimal SaldoDisponible { get; init; }
    public required bool Estado { get; init; }
    public required string ClienteId { get; init; }
    public required DateTime CreadoEn { get; init; }
}

public sealed record CuentaQuery
{
    private const int TamanoMaximo = 100;

    public string? ClienteId { get; init; }
    public TipoCuenta? TipoCuenta { get; init; }
    public bool? Estado { get; init; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, TamanoMaximo)]
    public int TamanoPagina { get; init; } = 20;

    public int PaginaSegura => Pagina < 1 ? 1 : Pagina;

    public int TamanoSeguro => TamanoPagina is < 1 or > TamanoMaximo ? 20 : TamanoPagina;
}
