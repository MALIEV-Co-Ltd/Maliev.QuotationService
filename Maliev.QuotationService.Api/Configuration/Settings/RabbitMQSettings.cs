namespace Maliev.QuotationService.Api.Configuration.Settings;

/// <summary>
/// RabbitMQ connection settings.
/// </summary>
public class RabbitMQSettings
{
    /// <summary>
    /// Whether RabbitMQ is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// The RabbitMQ host.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    /// <summary>
    /// The RabbitMQ port.
    /// </summary>
    public int Port { get; set; } = 5672;
    /// <summary>
    /// The RabbitMQ username.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>
    /// The RabbitMQ password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
    /// <summary>
    /// The RabbitMQ virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";
}
