namespace Maliev.QuotationService.Api.ExternalClients;

/// <summary>
/// Indicates that a linked project is absent or does not belong to the quotation customer.
/// </summary>
public sealed class ProjectNotFoundException : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance for the requested project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    public ProjectNotFoundException(Guid projectId)
        : base($"Project with ID {projectId} not found")
    {
    }
}
