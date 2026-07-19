namespace Maliev.QuotationService.Api.Configuration.Settings;

/// <summary>
/// Redis cache settings.
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Whether Redis is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// The Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
