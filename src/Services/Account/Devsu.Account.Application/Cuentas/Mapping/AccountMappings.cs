using Devsu.Account.Application.Cuentas.Dtos;
using Devsu.Account.Application.Movimientos.Dtos;
using Devsu.Account.Domain.Entities;

namespace Devsu.Account.Application.Cuentas.Mapping;

/// <summary>Mapeo manual</summary>
public static class AccountMappings
{
    public static CuentaResponse ToResponse(this Cuenta cuenta) => new()
    {
        NumeroCuenta = cuenta.NumeroCuenta,
        TipoCuenta = cuenta.TipoCuenta.ToString(),
        SaldoInicial = cuenta.SaldoInicial,
        SaldoDisponible = cuenta.SaldoDisponible,
        Estado = cuenta.Estado,
        ClienteId = cuenta.ClienteId,
        CreadoEn = cuenta.CreadoEn
    };

    public static MovimientoResponse ToResponse(this Movimiento movimiento, string numeroCuenta) => new()
    {
        MovimientoId = movimiento.MovimientoId,
        NumeroCuenta = numeroCuenta,
        Fecha = movimiento.Fecha,
        TipoMovimiento = movimiento.TipoMovimiento.ToString(),
        Valor = movimiento.Valor,
        Saldo = movimiento.Saldo,
        RegistradoEn = movimiento.RegistradoEn
    };
}
