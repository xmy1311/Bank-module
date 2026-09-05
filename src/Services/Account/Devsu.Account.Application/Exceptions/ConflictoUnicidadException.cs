namespace Devsu.Account.Application.Exceptions;

/// <summary>
/// Violación de índice único, traducida a un tipo de la Application.
/// Comprobar la existencia y luego insertar no es atómico: dos peticiones
/// concurrentes pasan ambas la comprobación y la segunda choca contra el UNIQUE.
/// </summary>
public sealed class ConflictoUnicidadException : Exception
{
    public ConflictoUnicidadException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
