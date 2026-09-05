using System.Text.Json.Serialization;
using Devsu.Customer.Api.Middleware;
using Devsu.Customer.Infrastructure;
using Devsu.Customer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);


builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

// Servicios
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Convierte los errores de validación de DataAnnotations en ValidationProblemDetails (RFC 7807).
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Devsu · Customer Service",
        Version = "v1",
        Description =
            "Microservicio de Clientes y Personas.\n\n" +
            "NOTA: DELETE /api/clientes/{clienteId} realiza una BAJA LÓGICA. " +
            "El cliente tiene información financiera en el Account Service; un borrado " +
            "físico dejaría cuentas huérfanas y destruiría la trazabilidad."
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();


app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer Service v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

// Dos endpoints: /health (liveness) no toca la base de datos,
// /health/ready sí comprueba las dependencias.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

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

// Migraciones al arrancar 
await AplicarMigracionesAsync(app);

app.Run();

static async Task AplicarMigracionesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

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
            "y que la variable ConnectionStrings__CustomerDb apunte al host y puerto correctos.");

        throw;
    }
}

/// Expuesto para que WebApplicationFactory pueda arrancar la API en los tests.
public partial class Program;
