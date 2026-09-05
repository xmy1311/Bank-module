using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Devsu.Shared.Messaging;

/// <summary>
/// Comprueba la conexión REAL al broker abriendo un canal.
/// </summary>
public sealed class RabbitMqHealthCheck : IHealthCheck
{
  
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IRabbitMqConnection _conexion;

    public RabbitMqHealthCheck(IRabbitMqConnection conexion) => _conexion = conexion;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        try
        {
            await using var canal = await _conexion.CrearCanalAsync(cts.Token);

            return canal.IsOpen
                ? HealthCheckResult.Healthy("Conexión a RabbitMQ establecida.")
                : HealthCheckResult.Unhealthy("El canal de RabbitMQ no está abierto.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"RabbitMQ no respondió en {Timeout.TotalSeconds:0} segundos.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al conectar con RabbitMQ.", ex);
        }
    }
}
