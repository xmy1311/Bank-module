using Devsu.Account.Domain.Common;
using Devsu.Account.Domain.Enums;

namespace Devsu.Account.Domain.Entities;

/// <summary>
/// Un movimiento erróneo se corrige con un reverso, no con un UPDATE.
/// </summary>
public class Movimiento
{
    public Guid MovimientoId { get; private set; }
    public Guid CuentaId { get; private set; }

    public DateTime Fecha { get; private set; }

    public TipoMovimiento TipoMovimiento { get; private set; }
  
    public decimal Valor { get; private set; } //Con signo positivo: depósito, negativo: retiro.
   
    public decimal Saldo { get; private set; }

    public DateTime RegistradoEn { get; private set; }

    private Movimiento()
    {
    }

    internal Movimiento(Guid cuentaId, decimal valor, decimal saldoResultante, DateTime fecha)
    {
        MovimientoId = SecuencialGuid.Nuevo();
        CuentaId = cuentaId;
        Valor = valor;
        Saldo = saldoResultante;
        Fecha = fecha;
        TipoMovimiento = valor > 0 ? TipoMovimiento.Deposito : TipoMovimiento.Retiro;
        RegistradoEn = DateTime.UtcNow;
    }
}
