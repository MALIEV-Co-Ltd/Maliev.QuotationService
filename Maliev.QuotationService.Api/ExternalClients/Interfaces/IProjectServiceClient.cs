namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

/// <summary>
/// Describes the authoritative result of a ProjectService ownership lookup.
/// </summary>
public enum ProjectOwnershipStatus
{
    /// <summary>The project exists and belongs to the expected customer.</summary>
    Owned,

    /// <summary>The project does not exist or belongs to another customer.</summary>
    NotOwned,

    /// <summary>ProjectService could not provide a trustworthy ownership decision.</summary>
    Unavailable
}

/// <summary>
/// Contains an ownership decision and the authoritative project number when owned.
/// </summary>
/// <param name="Status">The ownership decision.</param>
/// <param name="ProjectNumber">The ProjectService project number when <paramref name="Status"/> is owned.</param>
public sealed record ProjectOwnershipResult(ProjectOwnershipStatus Status, string? ProjectNumber = null);

/// <summary>
/// Provides authoritative ProjectService ownership checks for linked quotations.
/// </summary>
public interface IProjectServiceClient
{
    /// <summary>
    /// Verifies that a project exists and belongs to the expected customer.
    /// </summary>
    /// <param name="projectId">The project identifier to verify.</param>
    /// <param name="customerId">The expected owning customer.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The authoritative ownership decision.</returns>
    Task<ProjectOwnershipResult> VerifyOwnershipAsync(
        Guid projectId,
        Guid customerId,
        CancellationToken cancellationToken = default);
}
