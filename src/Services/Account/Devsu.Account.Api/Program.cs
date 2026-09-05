using System.Text.Json.Serialization;
using Devsu.Account.Api.Middleware;
using Devsu.Account.Infrastructure;
using Devsu.Account.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Devsu · Account Service",
        Version = "v1",
        Description =
            "Microservicio de Cuentas y Movimientos.\n\n" +
            "· Los movimientos NO tienen Update: un asiento contable es inmutable y se corrige con un reverso.\n" +
            "· Saldo insuficiente responde 422 con el código SALDO_NO_DISPONIBLE y el mensaje \"Saldo no disponible\".\n" +
            "· /api/reportes acepta el contrato limpio (fechaInicio, fechaFin, clienteId) y el alias del enunciado (fecha, cliente)."
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

// Dos endpoints: /health (liveness) no toca la base de datos,
// /health/ready sí comprueba las dependencias.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            duracionMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                nombre = e.Key,
                estado = e.Value.Status.ToString(),
                descripcion = e.Value.Description
            })
        });
    }
});

await AplicarMigracionesAsync(app);

app.Run();

static async Task AplicarMigracionesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

    try
    {
        var pendientes = (await context.Database.GetPendingMigrationsAsync()).ToList();

        if (pendientes.Count == 0)
        {
            logger.LogInformation("Sin migraciones pendientes. Esquema al día.");
            return;
        }

        logger.LogInformation("Aplicando {Cantidad} migración(es): {Migraciones}",
            pendientes.Count, string.Join(", ", pendientes));

        await context.Database.MigrateAsync();

        logger.LogInformation("Migraciones aplicadas.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(
            ex,
            "No se pudo preparar la base de datos. Revisa que SQL Server esté accesible " +
            "y que la variable ConnectionStrings__AccountDb apunte al host y puerto correctos.");

        throw;
    }
}

/// <summary>Expuesto para que WebApplicationFactory pueda arrancar la API en los tests.</summary>
public partial class Program;
