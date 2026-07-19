namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

/// <summary>
/// Client interface for interacting with the IAM Service API.
/// </summary>
public interface IIAMServiceClient
{
    // No methods needed yet if I use HttpClient directly in RegistrationService,
    // but better to have it for future permission checks if we move away from JWT-only.
    // For now, I'll keep it simple and maybe just use HttpClient in the hosted service as seen in Accounting.
}
