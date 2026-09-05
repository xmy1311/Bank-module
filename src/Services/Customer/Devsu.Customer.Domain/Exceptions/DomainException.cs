namespace Devsu.Customer.Domain.Exceptions;


public class DomainException : Exception
{
    public string Codigo { get; }

    public DomainException(string codigo, string mensaje) : base(mensaje) => Codigo = codigo;
}

public sealed class ClienteDuplicadoException : DomainException
{
    public ClienteDuplicadoException(string identificacion)
        : base("CLIENTE_DUPLICADO",
               $"Ya existe una persona registrada con la identificación '{identificacion}'.")
    {
    }
}

public sealed class EntidadNoEncontradaException : DomainException
{
    public EntidadNoEncontradaException(string entidad, string clave)
        : base("ENTIDAD_NO_ENCONTRADA", $"No se encontró {entidad} con identificador '{clave}'.")
    {
    }
}
