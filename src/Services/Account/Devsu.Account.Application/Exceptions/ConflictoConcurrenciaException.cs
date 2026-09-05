namespace Devsu.Account.Application.Exceptions;

/// <summary>
/// DbUpdateConcurrencyException traducida a un tipo de la Application, para que
/// la capa de negocio no tenga que conocer EF Core.
/// </summary>
public sealed class ConflictoConcurrenciaException : Exception
{
    public ConflictoConcurrenciaException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
