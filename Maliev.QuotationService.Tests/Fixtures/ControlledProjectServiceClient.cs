using System.Collections.Concurrent;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Tests.Fixtures;

public sealed class ControlledProjectServiceClient : IProjectServiceClient
{
    private readonly ConcurrentQueue<ProjectOwnershipRequest> _requests = new();
    private Func<Guid, Guid, ProjectOwnershipResult> _resolve = Unavailable;

    public IReadOnlyCollection<ProjectOwnershipRequest> Requests => _requests.ToArray();

    public Task<ProjectOwnershipResult> VerifyOwnershipAsync(
        Guid projectId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _requests.Enqueue(new ProjectOwnershipRequest(projectId, customerId));
        return Task.FromResult(_resolve(projectId, customerId));
    }

    public void SetOwned(Guid projectId, Guid customerId, string projectNumber)
    {
        _resolve = (actualProjectId, actualCustomerId) =>
            actualProjectId == projectId && actualCustomerId == customerId
                ? new ProjectOwnershipResult(ProjectOwnershipStatus.Owned, projectNumber)
                : new ProjectOwnershipResult(ProjectOwnershipStatus.Unavailable);
    }

    public void SetForeign() =>
        _resolve = (_, _) => new ProjectOwnershipResult(ProjectOwnershipStatus.NotOwned);

    public void SetMissing() =>
        _resolve = (_, _) => new ProjectOwnershipResult(ProjectOwnershipStatus.NotOwned);

    public void SetUnavailable() =>
        _resolve = (_, _) => new ProjectOwnershipResult(ProjectOwnershipStatus.Unavailable);

    public void Reset()
    {
        while (_requests.TryDequeue(out _))
        {
        }

        _resolve = Unavailable;
    }

    private static ProjectOwnershipResult Unavailable(Guid projectId, Guid customerId) =>
        new(ProjectOwnershipStatus.Unavailable);

    public sealed record ProjectOwnershipRequest(Guid ProjectId, Guid CustomerId);
}
