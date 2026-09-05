using Devsu.Account.Application.Interfaces;
using Devsu.Account.Application.Common;
using Devsu.Account.Application.Cuentas;
using Devsu.Account.Application.Movimientos;
using Devsu.Account.Infrastructure.Health;
using Devsu.Account.Infrastructure.Persistence;
using Devsu.Account.Infrastructure.Persistence.Queries;
using Devsu.Account.Application.Integracion;
using Devsu.Account.Infrastructure.Messaging;
using Devsu.Account.Infrastructure.Persistence.Repositories;
using Devsu.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devsu.Account.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AccountDb");

        // IsNullOrWhiteSpace, NO solo null: appsettings.json declara la clave con
        // cadena VACÍA para documentarla, así que GetConnectionString devuelve ""
        // y un simple "?? throw" no salta. EF Core llegaría hasta SqlClient y
        // fallaría con "The ConnectionString property has not been initialized",
        // que no le dice nada a nadie.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión 'AccountDb'. Defínela con la variable de entorno " +
                "ConnectionStrings__AccountDb (ver .env.example). Ejemplo en PowerShell:\n" +
                "  $env:ConnectionStrings__AccountDb=\"Server=localhost,14340;Database=AccountDb;" +
                "User Id=sa;Password=***;TrustServerCertificate=True;Encrypt=True\"");
        }

        services.AddDbContext<AccountDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                sql.CommandTimeout(30);
            }));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AccountDbContext>());
        services.AddScoped<ICuentaRepository, CuentaRepository>();
        services.AddScoped<IClienteReplicaRepository, ClienteReplicaRepository>();
        services.AddScoped<IEventoProcesadoRepository, EventoProcesadoRepository>();
        services.AddScoped<IMovimientoQueryService, MovimientoQueryService>();
        services.AddScoped<IReporteQueryService, ReporteQueryService>();
        services.AddScoped<ICuentaService, CuentaService>();
        services.AddScoped<IMovimientoService, MovimientoService>();
        services.AddScoped<CorrelationContext>();

        // Mensajería: conexión singleton, health check y consumidor en background.
        services.AddRabbitMq(configuration);
        services.AddScoped<IClienteSyncService, ClienteSyncService>();
        services.AddHostedService<ClienteEventConsumer>();

        services.AddScoped<SqlServerHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sqlserver", tags: ["ready"]);

        return services;
    }
}
