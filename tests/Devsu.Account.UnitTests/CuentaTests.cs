using Devsu.Account.Domain.Entities;
using Devsu.Account.Domain.Enums;
using Devsu.Account.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Devsu.Account.UnitTests;


public class CuentaTests
{
    private static Cuenta CrearCuenta(decimal saldoInicial = 2000m, string numero = "478758")
        => new(numero, TipoCuenta.Ahorros, saldoInicial, "CLI-0001");


    [Fact]
    public void RegistrarMovimiento_SinSaldoSuficiente_LanzaSaldoNoDisponible()
    {
        var cuenta = CrearCuenta(saldoInicial: 100m);

        var ex = Should.Throw<SaldoNoDisponibleException>(
            () => cuenta.RegistrarMovimiento(-575m, DateTime.UtcNow));

        ex.Message.ShouldBe("Saldo no disponible");
        ex.Codigo.ShouldBe("SALDO_NO_DISPONIBLE");
    }


    [Fact]
    public void RegistrarMovimiento_CuandoFalla_NoDejaEfectosSecundarios()
    {
        var cuenta = CrearCuenta(saldoInicial: 100m);

        Should.Throw<SaldoNoDisponibleException>(() => cuenta.RegistrarMovimiento(-575m, DateTime.UtcNow));

        cuenta.SaldoDisponible.ShouldBe(100m);
        cuenta.SaldoInicial.ShouldBe(100m);
        cuenta.Movimientos.ShouldBeEmpty();
    }


    [Fact]
    public void RegistrarMovimiento_QueDejaElSaldoEnCero_EsValido()
    {
        var cuenta = CrearCuenta(saldoInicial: 540m, numero: "496825");

        var movimiento = cuenta.RegistrarMovimiento(-540m, DateTime.UtcNow);

        cuenta.SaldoDisponible.ShouldBe(0m);
        movimiento.Saldo.ShouldBe(0m);
    }

    [Fact]
    public void RegistrarMovimiento_UnDepositoNuncaFallaPorSaldo()
    {
        var cuenta = CrearCuenta(saldoInicial: 0m, numero: "495878");

        var movimiento = cuenta.RegistrarMovimiento(150m, DateTime.UtcNow);

        movimiento.TipoMovimiento.ShouldBe(TipoMovimiento.Deposito);
        cuenta.SaldoDisponible.ShouldBe(150m);
    }



    [Fact]
    public void RegistrarMovimiento_MantieneLaEcuacionDelSaldo()
    {
        var cuenta = CrearCuenta(saldoInicial: 2000m);

        cuenta.RegistrarMovimiento(-575m, DateTime.UtcNow);
        cuenta.RegistrarMovimiento(300m, DateTime.UtcNow);
        cuenta.RegistrarMovimiento(-125m, DateTime.UtcNow);

        cuenta.SaldoDisponible.ShouldBe(1600m);

        cuenta.SaldoInicial.ShouldBe(2000m);

        cuenta.SaldoDisponible.ShouldBe(cuenta.SaldoInicial + cuenta.Movimientos.Sum(m => m.Valor));

        cuenta.Movimientos.Last().Saldo.ShouldBe(1600m);
    }


    [Theory]
    [InlineData(600, TipoMovimiento.Deposito)]
    [InlineData(-575, TipoMovimiento.Retiro)]
    public void RegistrarMovimiento_DerivaElTipoDelSignoDelValor(decimal valor, TipoMovimiento esperado)
    {
        var cuenta = CrearCuenta(saldoInicial: 2000m);

        var movimiento = cuenta.RegistrarMovimiento(valor, DateTime.UtcNow);

        movimiento.TipoMovimiento.ShouldBe(esperado);
        movimiento.Valor.ShouldBe(valor);
    }


    [Fact]
    public void RegistrarMovimiento_ConValorCero_Lanza()
    {
        var cuenta = CrearCuenta();

        var ex = Should.Throw<DomainException>(() => cuenta.RegistrarMovimiento(0m, DateTime.UtcNow));

        ex.Codigo.ShouldBe("MOVIMIENTO_INVALIDO");
    }

    [Fact]
    public void RegistrarMovimiento_EnCuentaInactiva_Lanza()
    {
        var cuenta = CrearCuenta();
        cuenta.Desactivar();

        var ex = Should.Throw<DomainException>(() => cuenta.RegistrarMovimiento(100m, DateTime.UtcNow));

        ex.Codigo.ShouldBe("CUENTA_INACTIVA");
    }

    [Fact]
    public void RegistrarMovimiento_ConFechaFutura_Lanza()
    {
        var cuenta = CrearCuenta();

        var ex = Should.Throw<DomainException>(
            () => cuenta.RegistrarMovimiento(100m, DateTime.UtcNow.AddDays(1)));

        ex.Codigo.ShouldBe("FECHA_INVALIDA");
    }

    [Fact]
    public void Constructor_ConSaldoInicialNegativo_Lanza()
    {
        var ex = Should.Throw<DomainException>(
            () => new Cuenta("478758", TipoCuenta.Ahorros, -1m, "CLI-0001"));

        ex.Codigo.ShouldBe("SALDO_INICIAL_INVALIDO");
    }


    [Fact]
    public void Movimientos_SeExponeComoSoloLectura()
    {
        var cuenta = CrearCuenta();

        cuenta.Movimientos.ShouldBeAssignableTo<IReadOnlyCollection<Movimiento>>();
        (cuenta.Movimientos is ICollection<Movimiento> { IsReadOnly: false }).ShouldBeFalse();
    }
}
