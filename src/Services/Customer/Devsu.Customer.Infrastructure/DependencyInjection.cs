using Devsu.Customer.Application.Interfaces;
using Devsu.Customer.Application.Clientes;
using Devsu.Customer.Application.Common;
using Devsu.Customer.Domain.Services;
using Devsu.Customer.Infrastructure.Health;
using Devsu.Customer.Infrastructure.Messaging;
using Devsu.Customer.Infrastructure.Persistence;
using Devsu.Customer.Infrastructure.Persistence.Repositories;
using Devsu.Customer.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Devsu.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Devsu.Customer.Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CustomerDb");

  
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión 'CustomerDb'. Defínela con la variable de entorno " +
                "ConnectionStrings__CustomerDb (ver .env.example). Ejemplo en PowerShell:\n" +
                "  $env:ConnectionStrings__CustomerDb=\"Server=localhost,14340;Database=CustomerDb;" +
                "User Id=sa;Password=***;TrustServerCertificate=True;Encrypt=True\"");
        }

        services.AddDbContext<CustomerDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                // Resiliencia
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                sql.CommandTimeout(30);
            }));

        // Scoped: una unidad de trabajo por petición HTTP.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CustomerDbContext>());
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IClienteIdGenerator, ClienteIdGenerator>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<CorrelationContext>();

        // Singleton: sin estado y sin dependencias con ciclo de vida menor.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // Mensajería: conexión singleton + health check etiquetado como "ready".
        services.AddRabbitMq(configuration);
        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();

        services.AddScoped<SqlServerHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sqlserver", tags: ["ready"]);

        return services;
    }
}
