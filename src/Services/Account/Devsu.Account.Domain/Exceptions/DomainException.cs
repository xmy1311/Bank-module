namespace Devsu.Account.Domain.Exceptions;

/// <summary>
/// Excepción de regla de negocio con un <see cref="Codigo"/> estable.
/// </summary>
public class DomainException : Exception
{
    public string Codigo { get; }

    public DomainException(string codigo, string mensaje) : base(mensaje) => Codigo = codigo;
}


public sealed class SaldoNoDisponibleException : DomainException
{
    public const string CodigoError = "SALDO_NO_DISPONIBLE";

    public string NumeroCuenta { get; }
    public decimal SaldoActual { get; }
    public decimal ValorSolicitado { get; }

    public SaldoNoDisponibleException(string numeroCuenta, decimal saldoActual, decimal valorSolicitado)
        : base(CodigoError, "Saldo no disponible")
    {
        NumeroCuenta = numeroCuenta;
        SaldoActual = saldoActual;
        ValorSolicitado = valorSolicitado;
    }
}

public sealed class CuentaDuplicadaException : DomainException
{
    public CuentaDuplicadaException(string numeroCuenta)
        : base("CUENTA_DUPLICADA", $"Ya existe una cuenta con el número '{numeroCuenta}'.")
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

public sealed class ClienteInactivoException : DomainException
{
    public ClienteInactivoException(string clienteId)
        : base("CLIENTE_INACTIVO", $"El cliente '{clienteId}' está inactivo y no puede operar cuentas.")
    {
    }
}
