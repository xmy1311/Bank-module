using Devsu.Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Devsu.Account.Infrastructure.Health;

public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly AccountDbContext _context;

    public SqlServerHealthCheck(AccountDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Conexión a SQL Server establecida.")
                : HealthCheckResult.Unhealthy("No se pudo conectar a SQL Server.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al conectar con SQL Server.", ex);
        }
    }
}
