using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devsu.Shared.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SeccionConfiguracion));

        // Singleton: una conexión TCP por proceso. 
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<RabbitMqHealthCheck>();

        services.AddHealthChecks()
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

        return services;
    }
}
