namespace Maliev.QuotationService.Api.Configuration.Settings;

public class RedisSettings
{
    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = string.Empty;
}
