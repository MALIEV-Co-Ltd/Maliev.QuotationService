namespace Maliev.QuotationService.Api.ExternalClients;

/// <summary>
/// Indicates that ProjectService could not provide an authoritative ownership decision.
/// </summary>
public sealed class ProjectServiceUnavailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the exception.
    /// </summary>
    public ProjectServiceUnavailableException()
        : base("Project service unavailable.")
    {
    }
}
