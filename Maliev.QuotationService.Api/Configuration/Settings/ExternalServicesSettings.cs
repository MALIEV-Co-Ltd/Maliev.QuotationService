namespace Maliev.QuotationService.Api.Configuration.Settings;

public class ExternalServicesSettings
{
    public ServiceEndpoint Currency { get; set; } = new();
    public ServiceEndpoint Material { get; set; } = new();
    public ServiceEndpoint Upload { get; set; } = new();
    public ServiceEndpoint PDF { get; set; } = new();
}

public class ServiceEndpoint
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutInSeconds { get; set; } = 30;
}
