using Devsu.Customer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Devsu.Customer.Infrastructure.Health;

public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly CustomerDbContext _context;

    public SqlServerHealthCheck(CustomerDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conectado = await _context.Database.CanConnectAsync(cancellationToken);

            return conectado
                ? HealthCheckResult.Healthy("Conexión a SQL Server establecida.")
                : HealthCheckResult.Unhealthy("No se pudo conectar a SQL Server.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al conectar con SQL Server.", ex);
        }
    }
}
