using Devsu.Account.Infrastructure.Messaging;
using Devsu.Account.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using Microsoft.AspNetCore.TestHost;
using Xunit;


namespace Devsu.Account.IntegrationTests;

/// <summary>
/// Estas pruebas verifican la API y la  persistencia
/// </summary>
public sealed class AccountApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration#Tests2026!")
        .Build();

    public async Task InitializeAsync() => await _sqlServer.StartAsync();

    public new async Task DisposeAsync()
    {
        await _sqlServer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = _sqlServer.GetConnectionString()
            .Replace("Database=master", "Database=AccountDb", StringComparison.OrdinalIgnoreCase);

        builder.UseSetting("ConnectionStrings:AccountDb", connectionString);
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Reapuntar el DbContext al contenedor.
            services.RemoveAll<DbContextOptions<AccountDbContext>>();
            services.RemoveAll<AccountDbContext>();

            services.AddDbContext<AccountDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Sin broker en esta prueba.
            var consumidor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IHostedService)
                     && d.ImplementationType == typeof(ClienteEventConsumer));

            if (consumidor is not null)
            {
                services.Remove(consumidor);
            }

    
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                var rabbit = options.Registrations.FirstOrDefault(r => r.Name == "rabbitmq");

                if (rabbit is not null)
                {
                    options.Registrations.Remove(rabbit);
                }
            });
        });
    }
}
