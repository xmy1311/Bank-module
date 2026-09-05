using Devsu.Account.Domain.Entities;
using Shouldly;
using Xunit;

namespace Devsu.Account.UnitTests;

public class ClienteReplicaTests
{
    private static readonly DateTime T1 = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc);

    private static ClienteReplica Crear()
        => new("CLI-0001", "Jose Lema", "1712345678", estado: true, occurredOn: T1);

    [Fact]
    public void Aplicar_ConEventoMasReciente_ActualizaYDevuelveTrue()
    {
        var replica = Crear();

        var aplicado = replica.Aplicar("Jose Lema Actualizado", "1712345678", estado: true, occurredOn: T2);

        aplicado.ShouldBeTrue();
        replica.Nombre.ShouldBe("Jose Lema Actualizado");
        replica.ActualizadoEn.ShouldBe(T2);
    }

    [Fact]
    public void Aplicar_ConEventoOBSOLETO_NoModificaNadaYDevuelveFalse()
    {
        var replica = Crear();
        replica.Aplicar("Nombre nuevo", "1712345678", estado: true, occurredOn: T2);

        // Llega tarde un evento anterior (reintento tras un fallo).
        var aplicado = replica.Aplicar("Nombre viejo", "1712345678", estado: false, occurredOn: T1);

        aplicado.ShouldBeFalse();
        replica.Nombre.ShouldBe("Nombre nuevo");
        replica.Estado.ShouldBeTrue();
        replica.ActualizadoEn.ShouldBe(T2);
    }

    [Fact]
    public void Aplicar_ConElMismoTimestamp_EsIdempotente()
    {
        var replica = Crear();

        var aplicado = replica.Aplicar("Otro nombre", "9999999999", estado: false, occurredOn: T1);

        aplicado.ShouldBeFalse();
        replica.Nombre.ShouldBe("Jose Lema");
        replica.Estado.ShouldBeTrue();
    }

    [Fact]
    public void Aplicar_ConDesactivacion_PropagaElEstado()
    {
        var replica = Crear();

        replica.Aplicar("Jose Lema", "1712345678", estado: false, occurredOn: T2);

        replica.Estado.ShouldBeFalse();
    }
}
