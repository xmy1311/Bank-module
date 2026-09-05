namespace Devsu.Shared.Messaging;

/// <summary>
/// Configuración del broker. 
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SeccionConfiguracion = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string Usuario { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public int MaxIntentosConexion { get; set; } = 10;
   
    public int SegundosBackoffBase { get; set; } = 2;
}
