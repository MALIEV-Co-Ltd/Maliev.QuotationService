namespace Maliev.QuotationService.Api.Configuration.Settings;

/// <summary>
/// JWT authentication settings.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// The public key for JWT token validation.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
    /// <summary>
    /// The JWT token issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
    /// <summary>
    /// The JWT token audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;
}
