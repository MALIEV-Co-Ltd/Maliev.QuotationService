namespace Maliev.QuotationService.Api.Configuration.Settings;

public class JwtSettings
{
    public string PublicKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
