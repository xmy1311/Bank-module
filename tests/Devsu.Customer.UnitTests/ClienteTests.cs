using Devsu.Customer.Domain.Entities;
using Devsu.Customer.Domain.Enums;
using Devsu.Customer.Domain.Exceptions;
using Devsu.Customer.Domain.Services;
using Devsu.Customer.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace Devsu.Customer.UnitTests;

public class ClienteTests
{
    private static readonly IPasswordHasher Hasher = new Pbkdf2PasswordHasher();

    private static Cliente CrearCliente(
        string clienteId = "CLI-0001",
        string identificacion = "1712345678",
        string password = "1234")
        => new(
            clienteId,
            nombre: "Jose Lema",
            genero: Genero.Masculino,
            edad: 35,
            identificacion: identificacion,
            direccion: "Otavalo sn y principal",
            telefono: "098254785",
            passwordPlano: password,
            hasher: Hasher);


    [Fact]
    public void EstablecerPassword_NuncaAlmacenaLaContrasenaEnClaro()
    {
        var cliente = CrearCliente(password: "1234");

        cliente.PasswordHash.ShouldNotBe("1234");
        cliente.PasswordHash.ShouldNotContain("1234");
        cliente.PasswordSalt.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void VerificarPassword_DistingueLaCorrectaDeLaIncorrecta()
    {
        var cliente = CrearCliente(password: "1234");

        cliente.VerificarPassword("1234", Hasher).ShouldBeTrue();
        cliente.VerificarPassword("4321", Hasher).ShouldBeFalse();
        cliente.VerificarPassword("", Hasher).ShouldBeFalse();
    }


    [Fact]
    public void DosClientesConLaMismaContrasena_ProducenHashesDistintos()
    {
        var a = CrearCliente("CLI-0001", "1712345678", "1234");
        var b = CrearCliente("CLI-0002", "0923456789", "1234");

        a.PasswordSalt.ShouldNotBe(b.PasswordSalt);
        a.PasswordHash.ShouldNotBe(b.PasswordHash);

        a.VerificarPassword("1234", Hasher).ShouldBeTrue();
        b.VerificarPassword("1234", Hasher).ShouldBeTrue();
    }

    [Fact]
    public void CambiarPassword_InvalidaLaAnterior()
    {
        var cliente = CrearCliente(password: "1234");

        cliente.EstablecerPassword("nuevaClave", Hasher);

        cliente.VerificarPassword("1234", Hasher).ShouldBeFalse();
        cliente.VerificarPassword("nuevaClave", Hasher).ShouldBeTrue();
    }


    [Fact]
    public void Desactivar_MarcaInactivoYRegistraLaFecha()
    {
        var cliente = CrearCliente();

        cliente.Estado.ShouldBeTrue();
        cliente.DesactivadoEn.ShouldBeNull();

        cliente.Desactivar();

        cliente.Estado.ShouldBeFalse();
        cliente.DesactivadoEn.ShouldNotBeNull();
    }


    [Fact]
    public void Desactivar_EsIdempotente_ConservaLaFechaOriginal()
    {
        var cliente = CrearCliente();
        cliente.Desactivar();
        var primeraFecha = cliente.DesactivadoEn;

        cliente.Desactivar();

        cliente.Estado.ShouldBeFalse();
        cliente.DesactivadoEn.ShouldBe(primeraFecha);
    }

    [Fact]
    public void Reactivar_LimpiaLaFechaDeDesactivacion()
    {
        var cliente = CrearCliente();
        cliente.Desactivar();

        cliente.Reactivar();

        cliente.Estado.ShouldBeTrue();
        cliente.DesactivadoEn.ShouldBeNull();
    }



    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConIdentificacionVacia_Lanza(string identificacion)
    {
        var ex = Should.Throw<DomainException>(() => CrearCliente(identificacion: identificacion));

        ex.Codigo.ShouldBe("IDENTIFICACION_INVALIDA");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(151)]
    public void Constructor_ConEdadFueraDeRango_Lanza(int edad)
    {
        var ex = Should.Throw<DomainException>(() => new Cliente(
            "CLI-0001", "Jose Lema", Genero.Masculino, edad,
            "1712345678", "Otavalo sn", "098254785", "1234", Hasher));

        ex.Codigo.ShouldBe("EDAD_INVALIDA");
    }

    [Fact]
    public void Constructor_SinContrasena_Lanza()
    {
        var ex = Should.Throw<DomainException>(() => CrearCliente(password: "  "));

        ex.Codigo.ShouldBe("PASSWORD_REQUERIDA");
    }

    [Fact]
    public void Constructor_NormalizaEspaciosEnBlanco()
    {
        var cliente = new Cliente(
            "  CLI-0009  ", "  Ana Perez  ", Genero.Femenino, 30,
            "  1799887766  ", "  Calle 10  ", "  0991234567  ", "clave", Hasher);

        cliente.ClienteId.ShouldBe("CLI-0009");
        cliente.Nombre.ShouldBe("Ana Perez");
        cliente.Identificacion.ShouldBe("1799887766");
    }
}
