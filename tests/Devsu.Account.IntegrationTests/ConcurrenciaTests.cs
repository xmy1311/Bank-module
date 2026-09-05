using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace Devsu.Account.IntegrationTests;

public class ConcurrenciaTests : IClassFixture<AccountApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _cliente;

    public ConcurrenciaTests(AccountApiFactory factory) => _cliente = factory.CreateClient();

    [Fact]
    public async Task RetirosConcurrentes_NuncaDejanElSaldoNegativo()
    {
        const decimal saldoInicial = 1000m;
        const decimal montoRetiro = 200m;
        const int peticionesConcurrentes = 10;

        // Solo 5 de los 10 retiros caben en el saldo. Los otros 5 deben rechazarse.
        const int exitosEsperados = (int)(saldoInicial / montoRetiro);

        var numeroCuenta = $"CC{Random.Shared.Next(100000, 999999)}";

        var creacion = await _cliente.PostAsJsonAsync("/api/cuentas", new
        {
            numeroCuenta,
            tipoCuenta = "Ahorros",
            saldoInicial,
            clienteId = "CLI-0001"
        }, Json);

        creacion.StatusCode.ShouldBe(HttpStatusCode.Created);

      
        var peticiones = Enumerable.Range(0, peticionesConcurrentes).Select(_ =>
            _cliente.PostAsJsonAsync(
                "/api/movimientos",
                new { numeroCuenta, valor = -montoRetiro },
                Json));

        var respuestas = await Task.WhenAll(peticiones);

        var codigos = respuestas.Select(r => r.StatusCode).ToList();
        var exitosos = codigos.Count(c => c == HttpStatusCode.Created);
        var rechazados = codigos.Count(c => c == HttpStatusCode.UnprocessableEntity);
        var conflictos = codigos.Count(c => c == HttpStatusCode.Conflict);

        codigos.ShouldAllBe(c => c != HttpStatusCode.InternalServerError);
        (exitosos + rechazados + conflictos).ShouldBe(peticionesConcurrentes);

        var cuenta = await _cliente.GetFromJsonAsync<JsonElement>($"/api/cuentas/{numeroCuenta}", Json);
        var saldoFinal = cuenta.GetProperty("saldoDisponible").GetDecimal();

        saldoFinal.ShouldBe(saldoInicial - (exitosos * montoRetiro));

        saldoFinal.ShouldBeGreaterThanOrEqualTo(0m);

       
        exitosos.ShouldBeLessThanOrEqualTo(exitosEsperados);

        var movimientos = await _cliente.GetFromJsonAsync<JsonElement>(
            $"/api/movimientos?numeroCuenta={numeroCuenta}&tamanoPagina=100", Json);

        var registrados = movimientos.GetProperty("items").EnumerateArray().ToList();
        registrados.Count.ShouldBe(exitosos);

        var sumaMovimientos = registrados.Sum(m => m.GetProperty("valor").GetDecimal());
        saldoFinal.ShouldBe(saldoInicial + sumaMovimientos);
    }

    /// <summary>
    /// Depósitos concurrentes: ninguno debe perderse. 
    /// </summary>
    [Fact]
    public async Task DepositosConcurrentes_NoSePierdeNinguno()
    {
        const decimal saldoInicial = 0m;
        const decimal monto = 50m;
        const int peticiones = 8;

        var numeroCuenta = $"CD{Random.Shared.Next(100000, 999999)}";

        var creacion = await _cliente.PostAsJsonAsync("/api/cuentas", new
        {
            numeroCuenta,
            tipoCuenta = "Corriente",
            saldoInicial,
            clienteId = "CLI-0002"
        }, Json);

        creacion.StatusCode.ShouldBe(HttpStatusCode.Created);

        var respuestas = await Task.WhenAll(
            Enumerable.Range(0, peticiones).Select(_ =>
                _cliente.PostAsJsonAsync(
                    "/api/movimientos",
                    new { numeroCuenta, valor = monto },
                    Json)));

        var exitosos = respuestas.Count(r => r.StatusCode == HttpStatusCode.Created);

        var cuenta = await _cliente.GetFromJsonAsync<JsonElement>($"/api/cuentas/{numeroCuenta}", Json);

        cuenta.GetProperty("saldoDisponible").GetDecimal()
            .ShouldBe(exitosos * monto, "cada depósito aceptado debe estar reflejado en el saldo");
    }
}
