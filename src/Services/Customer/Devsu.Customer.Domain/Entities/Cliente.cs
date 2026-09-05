using Devsu.Customer.Domain.Enums;
using Devsu.Customer.Domain.Exceptions;
using Devsu.Customer.Domain.Services;

namespace Devsu.Customer.Domain.Entities;

public class Cliente : Persona
{
    public string ClienteId { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string PasswordSalt { get; private set; } = null!;
    public bool Estado { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime? ActualizadoEn { get; private set; }
    public DateTime? DesactivadoEn { get; private set; }

    private Cliente()
    {
    }

    public Cliente(
        string clienteId,
        string nombre,
        Genero genero,
        int edad,
        string identificacion,
        string direccion,
        string telefono,
        string passwordPlano,
        IPasswordHasher hasher)
        : base(nombre, genero, edad, identificacion, direccion, telefono)
    {
        if (string.IsNullOrWhiteSpace(clienteId))
        {
            throw new DomainException("CLIENTE_ID_REQUERIDO", "El identificador de cliente es obligatorio.");
        }

        ClienteId = clienteId.Trim();
        Estado = true;
        CreadoEn = DateTime.UtcNow;
        EstablecerPassword(passwordPlano, hasher);
        ActualizadoEn = null;
    }


    public void EstablecerPassword(string passwordPlano, IPasswordHasher hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);

        if (string.IsNullOrWhiteSpace(passwordPlano))
        {
            throw new DomainException("PASSWORD_REQUERIDA", "La contraseña es obligatoria.");
        }

        var (hash, salt) = hasher.Hash(passwordPlano);
        PasswordHash = hash;
        PasswordSalt = salt;
        ActualizadoEn = DateTime.UtcNow;
    }

    public bool VerificarPassword(string passwordPlano, IPasswordHasher hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);

        return !string.IsNullOrEmpty(passwordPlano)
               && hasher.Verify(passwordPlano, PasswordHash, PasswordSalt);
    }

    public override void ActualizarDatosPersonales(
        string nombre,
        Genero genero,
        int edad,
        string direccion,
        string telefono)
    {
        base.ActualizarDatosPersonales(nombre, genero, edad, direccion, telefono);
        ActualizadoEn = DateTime.UtcNow;
    }

    public void Desactivar()
    {
        if (!Estado)
        {
            return;
        }

        Estado = false;
        DesactivadoEn = DateTime.UtcNow;
        ActualizadoEn = DateTime.UtcNow;
    }

    public void Reactivar()
    {
        if (Estado)
        {
            return;
        }

        Estado = true;
        DesactivadoEn = null;
        ActualizadoEn = DateTime.UtcNow;
    }
}
