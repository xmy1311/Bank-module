using Devsu.Account.Domain.Common;
using Devsu.Account.Domain.Enums;
using Devsu.Account.Domain.Exceptions;

namespace Devsu.Account.Domain.Entities;

/// <summary>
/// Agregado raíz del Account Service; contiene sus movimientos.
/// </summary>
public class Cuenta
{
 
    public const decimal LimiteSobregiro = 0m;

    private readonly List<Movimiento> _movimientos = [];

    public Guid CuentaId { get; private set; }
    public string NumeroCuenta { get; private set; } = null!;
    public TipoCuenta TipoCuenta { get; private set; }

    public decimal SaldoInicial { get; private set; }

    public decimal SaldoDisponible { get; private set; }
    public bool Estado { get; private set; }
    public string ClienteId { get; private set; } = null!;
    public DateTime CreadoEn { get; private set; }
    public IReadOnlyCollection<Movimiento> Movimientos => _movimientos.AsReadOnly();

    private Cuenta()
    {
    }

    public Cuenta(string numeroCuenta, TipoCuenta tipoCuenta, decimal saldoInicial, string clienteId)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
        {
            throw new DomainException("NUMERO_CUENTA_REQUERIDO", "El número de cuenta es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(clienteId))
        {
            throw new DomainException("CLIENTE_ID_REQUERIDO", "El identificador de cliente es obligatorio.");
        }

        if (!Enum.IsDefined(tipoCuenta))
        {
            throw new DomainException("TIPO_CUENTA_INVALIDO", "El tipo de cuenta indicado no es válido.");
        }

        if (saldoInicial < 0)
        {
            throw new DomainException("SALDO_INICIAL_INVALIDO", "El saldo inicial no puede ser negativo.");
        }

        CuentaId = SecuencialGuid.Nuevo();
        NumeroCuenta = numeroCuenta.Trim();
        TipoCuenta = tipoCuenta;
        SaldoInicial = saldoInicial;
        SaldoDisponible = saldoInicial;   // coinciden SOLO en la apertura
        ClienteId = clienteId.Trim();
        Estado = true;
        CreadoEn = DateTime.UtcNow;
    }

  
    public Movimiento RegistrarMovimiento(decimal valor, DateTime fecha)
    {
        if (!Estado)
        {
            throw new DomainException(
                "CUENTA_INACTIVA",
                "No se pueden registrar movimientos en una cuenta inactiva.");
        }

        if (valor == 0)
        {
            throw new DomainException(
                "MOVIMIENTO_INVALIDO",
                "El valor del movimiento no puede ser cero.");
        }

        if (fecha > DateTime.UtcNow.AddMinutes(5))
        {
            throw new DomainException(
                "FECHA_INVALIDA",
                "La fecha del movimiento no puede ser futura.");
        }

        var nuevoSaldo = SaldoDisponible + valor;

        if (nuevoSaldo < LimiteSobregiro)
        {
            throw new SaldoNoDisponibleException(NumeroCuenta, SaldoDisponible, valor);
        }

        SaldoDisponible = nuevoSaldo;

        var movimiento = new Movimiento(CuentaId, valor, nuevoSaldo, fecha);
        _movimientos.Add(movimiento);

        return movimiento;
    }

    public void CambiarTipo(TipoCuenta tipoCuenta)
    {
        if (!Enum.IsDefined(tipoCuenta))
        {
            throw new DomainException("TIPO_CUENTA_INVALIDO", "El tipo de cuenta indicado no es válido.");
        }

        TipoCuenta = tipoCuenta;
    }

    public void Desactivar() => Estado = false;

    public void Activar() => Estado = true;
}
