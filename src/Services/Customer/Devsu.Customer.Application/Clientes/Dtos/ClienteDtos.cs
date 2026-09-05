using System.ComponentModel.DataAnnotations;
using Devsu.Customer.Domain.Enums;

namespace Devsu.Customer.Application.Clientes.Dtos;

// Los campos obligatorios de tipo valor van ANULABLES con [Required].
// Sobre un tipo no anulable el atributo no valida nada —un bool nunca es null—,
// así que omitir el campo lo dejaría en su valor por defecto sin dar error.


/// <summary>POST /api/clientes — el ClienteId lo genera el servidor.</summary>
public sealed record CrearClienteRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Nombre { get; init; } = null!;

    [Required]
    public Genero? Genero { get; init; }

    [Required]
    [Range(0, 150)]
    public int? Edad { get; init; }

    [Required, StringLength(20, MinimumLength = 5)]
    public string Identificacion { get; init; } = null!;

    [Required, StringLength(200, MinimumLength = 3)]
    public string Direccion { get; init; } = null!;

    [Required, StringLength(20, MinimumLength = 6)]
    public string Telefono { get; init; } = null!;

    [Required, StringLength(100, MinimumLength = 4)]
    public string Contrasena { get; init; } = null!;
}

/// <summary>PUT /api/clientes/{clienteId} — reemplazo completo de los datos modificables.</summary>
public sealed record ActualizarClienteRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Nombre { get; init; } = null!;

    [Required]
    public Genero? Genero { get; init; }

    [Required]
    [Range(0, 150)]
    public int? Edad { get; init; }

    [Required, StringLength(200, MinimumLength = 3)]
    public string Direccion { get; init; } = null!;

    [Required, StringLength(20, MinimumLength = 6)]
    public string Telefono { get; init; } = null!;

    [Required]
    public bool? Estado { get; init; }
}

/// <summary>PATCH /api/clientes/{clienteId} — solo los campos presentes se modifican.</summary>
public sealed record ActualizarParcialClienteRequest
{
    [StringLength(150, MinimumLength = 3)]
    public string? Nombre { get; init; }

    public Genero? Genero { get; init; }

    [Range(0, 150)]
    public int? Edad { get; init; }

    [StringLength(200, MinimumLength = 3)]
    public string? Direccion { get; init; }

    [StringLength(20, MinimumLength = 6)]
    public string? Telefono { get; init; }

    public bool? Estado { get; init; }
}

/// <summary>PATCH /api/clientes/{clienteId}/password — endpoint dedicado (RN-10).</summary>
public sealed record CambiarPasswordRequest
{
    [Required]
    public string ContrasenaActual { get; init; } = null!;

    [Required, StringLength(100, MinimumLength = 4)]
    public string ContrasenaNueva { get; init; } = null!;
}

/// <summary>
/// Respuesta pública de un cliente.
/// NO expone PasswordHash ni PasswordSalt: nunca salen del servicio.
/// </summary>
public sealed record ClienteResponse
{
    public required string ClienteId { get; init; }
    public required string Nombre { get; init; }
    public required string Genero { get; init; }
    public required int Edad { get; init; }
    public required string Identificacion { get; init; }
    public required string Direccion { get; init; }
    public required string Telefono { get; init; }
    public required bool Estado { get; init; }
    public required DateTime CreadoEn { get; init; }
    public DateTime? ActualizadoEn { get; init; }
}

/// <summary>Filtros y paginación de GET /api/clientes.</summary>
public sealed record ClienteQuery
{
    private const int TamanoMaximo = 100;

    public string? Nombre { get; init; }
    public string? Identificacion { get; init; }
    public bool? Estado { get; init; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, TamanoMaximo)]
    public int TamanoPagina { get; init; } = 20;

    public int PaginaSegura => Pagina < 1 ? 1 : Pagina;

    public int TamanoSeguro => TamanoPagina is < 1 or > TamanoMaximo ? 20 : TamanoPagina;
}
