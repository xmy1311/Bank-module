using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace Devsu.Account.IntegrationTests;

/// <summary>
/// prueba de integración.
/// Petición HTTP -> controlador -> servicio de
/// aplicación -> agregado -> EF Core -> SQL Server real -> respuesta.
/// </summary>
public class MovimientosIntegrationTests : IClassFixture<AccountApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _cliente;

    public MovimientosIntegrationTests(AccountApiFactory factory) => _cliente = factory.CreateClient();

    [Fact]
    public async Task HealthReady_RespondeHealthy_ConConexionRealALaBaseDeDatos()
    {
        var respuesta = await _cliente.GetAsync("/health/ready");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await respuesta.Content.ReadAsStringAsync()).ShouldContain("Healthy");
    }

    [Fact]
    public async Task GetCuentas_DevuelveElSeedDelEnunciado()
    {
        var respuesta = await _cliente.GetAsync("/api/cuentas?tamanoPagina=100");

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>(Json);

        json.GetProperty("totalRegistros").GetInt32().ShouldBeGreaterThanOrEqualTo(5);
        json.GetProperty("items").EnumerateArray()
            .Select(c => c.GetProperty("numeroCuenta").GetString())
            .ShouldContain("478758");
    }

    /// <summary>
    /// Un retiro válido actualiza el saldo.
    /// Si excede el saldo se rechaza con 422 "Saldo no disponible"
    /// </summary>
    [Fact]
    public async Task RegistrarMovimiento_ActualizaSaldo_YRechazaElQueExcede()
    {
        // La cuenta 585545 arranca con 1000 y sin movimientos en el seed.
        const string cuenta = "585545";

        var retiro = await _cliente.PostAsJsonAsync(
            "/api/movimientos", new { numeroCuenta = cuenta, valor = -400m }, Json);

        retiro.StatusCode.ShouldBe(HttpStatusCode.Created);

        var movimiento = await retiro.Content.ReadFromJsonAsync<JsonElement>(Json);
        movimiento.GetProperty("tipoMovimiento").GetString().ShouldBe("Retiro");
        movimiento.GetProperty("saldo").GetDecimal().ShouldBe(600m);

        var estado = await _cliente.GetFromJsonAsync<JsonElement>($"/api/cuentas/{cuenta}", Json);
        estado.GetProperty("saldoDisponible").GetDecimal().ShouldBe(600m);
        estado.GetProperty("saldoInicial").GetDecimal().ShouldBe(1000m); 

        var excedido = await _cliente.PostAsJsonAsync(
            "/api/movimientos", new { numeroCuenta = cuenta, valor = -5000m }, Json);

        excedido.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        excedido.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        var problema = await excedido.Content.ReadFromJsonAsync<JsonElement>(Json);
        problema.GetProperty("title").GetString().ShouldBe("Saldo no disponible");
        problema.GetProperty("code").GetString().ShouldBe("SALDO_NO_DISPONIBLE");

        var final = await _cliente.GetFromJsonAsync<JsonElement>($"/api/cuentas/{cuenta}", Json);
        final.GetProperty("saldoDisponible").GetDecimal().ShouldBe(600m);
    }

    [Fact]
    public async Task CrearCuenta_ParaClienteInexistente_Devuelve404()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/cuentas",
            new { numeroCuenta = "999001", tipoCuenta = "Ahorros", saldoInicial = 100m, clienteId = "CLI-9999" },
            Json);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>(Json);
        problema.GetProperty("code").GetString().ShouldBe("ENTIDAD_NO_ENCONTRADA");
    }

    [Fact]
    public async Task Reporte_FuncionaEnLasDosSintaxis_YReproduceElCasoDeUso5()
    {
        var limpia = await _cliente.GetFromJsonAsync<JsonElement>(
            "/api/reportes?fechaInicio=2022-02-01&fechaFin=2022-02-28&clienteId=CLI-0002", Json);

        var alias = await _cliente.GetFromJsonAsync<JsonElement>(
            "/api/reportes?fecha=2022-02-01,2022-02-28&cliente=CLI-0002", Json);

        foreach (var reporte in new[] { limpia, alias })
        {
            reporte.GetProperty("cliente").GetProperty("nombre").GetString()
                .ShouldBe("Marianela Montalvo");

            var cuentas = reporte.GetProperty("cuentas").EnumerateArray().ToList();
            cuentas.Count.ShouldBe(2);

            var corriente = cuentas.Single(c => c.GetProperty("numeroCuenta").GetString() == "225487");
            corriente.GetProperty("saldoInicial").GetDecimal().ShouldBe(100m);
            corriente.GetProperty("saldoDisponible").GetDecimal().ShouldBe(700m);

            var movimiento = corriente.GetProperty("movimientos").EnumerateArray().Single();
            movimiento.GetProperty("valor").GetDecimal().ShouldBe(600m);
            movimiento.GetProperty("saldo").GetDecimal().ShouldBe(700m);
        }
    }

    [Fact]
    public async Task RegistrarMovimiento_ConValorCero_Devuelve422()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/movimientos", new { numeroCuenta = "478758", valor = 0m }, Json);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>(Json);
        problema.GetProperty("code").GetString().ShouldBe("MOVIMIENTO_INVALIDO");
    }
}
