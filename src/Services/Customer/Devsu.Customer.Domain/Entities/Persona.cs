using Devsu.Customer.Domain.Common;
using Devsu.Customer.Domain.Enums;
using Devsu.Customer.Domain.Exceptions;

namespace Devsu.Customer.Domain.Entities;

public abstract class Persona
{
    public const int EdadMinima = 0;
    public const int EdadMaxima = 150;

    public Guid PersonaId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public Genero Genero { get; private set; }
    public int Edad { get; private set; }
    public string Identificacion { get; private set; } = null!;
    public string Direccion { get; private set; } = null!;
    public string Telefono { get; private set; } = null!;

    protected Persona()
    {
    }

    protected Persona(
        string nombre,
        Genero genero,
        int edad,
        string identificacion,
        string direccion,
        string telefono)
    {
        PersonaId = SecuencialGuid.Nuevo();
        EstablecerIdentificacion(identificacion);
        ActualizarDatosPersonales(nombre, genero, edad, direccion, telefono);
    }

    public virtual void ActualizarDatosPersonales(
        string nombre,
        Genero genero,
        int edad,
        string direccion,
        string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("NOMBRE_REQUERIDO", "El nombre es obligatorio.");
        }

        if (edad is < EdadMinima or > EdadMaxima)
        {
            throw new DomainException(
                "EDAD_INVALIDA",
                $"La edad debe estar entre {EdadMinima} y {EdadMaxima} años.");
        }

        if (!Enum.IsDefined(genero))
        {
            throw new DomainException("GENERO_INVALIDO", "El género indicado no es válido.");
        }

        if (string.IsNullOrWhiteSpace(direccion))
        {
            throw new DomainException("DIRECCION_REQUERIDA", "La dirección es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(telefono))
        {
            throw new DomainException("TELEFONO_REQUERIDO", "El teléfono es obligatorio.");
        }

        Nombre = nombre.Trim();
        Genero = genero;
        Edad = edad;
        Direccion = direccion.Trim();
        Telefono = telefono.Trim();
    }

  
    private void EstablecerIdentificacion(string identificacion)
    {
        if (string.IsNullOrWhiteSpace(identificacion) || identificacion.Trim().Length < 5)
        {
            throw new DomainException(
                "IDENTIFICACION_INVALIDA",
                "La identificación es obligatoria y debe tener al menos 5 caracteres.");
        }

        Identificacion = identificacion.Trim();
    }
}
