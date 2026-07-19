using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

/// <summary>
/// Authenticated HTTP client for authoritative ProjectService ownership checks.
/// </summary>
public sealed class ProjectServiceClient : IProjectServiceClient
{
    private static readonly TimeSpan OwnershipLookupTimeout = TimeSpan.FromSeconds(5);
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProjectServiceClient> _logger;

    /// <summary>
    /// Initializes a new ProjectService client.
    /// </summary>
    /// <param name="httpClient">The authenticated ProjectService HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public ProjectServiceClient(HttpClient httpClient, ILogger<ProjectServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProjectOwnershipResult> VerifyOwnershipAsync(
        Guid projectId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
        {
            return new ProjectOwnershipResult(ProjectOwnershipStatus.NotOwned);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(OwnershipLookupTimeout);

        try
        {
            using var response = await _httpClient.GetAsync(
                $"/project/v1/projects/{projectId:D}",
                timeout.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ProjectOwnershipResult(ProjectOwnershipStatus.NotOwned);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ProjectService ownership lookup for project {ProjectId} returned {StatusCode}.",
                    projectId,
                    response.StatusCode);
                return new ProjectOwnershipResult(ProjectOwnershipStatus.Unavailable);
            }

            await using var content = await response.Content.ReadAsStreamAsync(timeout.Token);
            var project = await JsonSerializer.DeserializeAsync<ProjectOwnershipPayload>(
                content,
                cancellationToken: timeout.Token);

            if (project is null ||
                project.Id == Guid.Empty ||
                project.Id != projectId ||
                project.CustomerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(project.ProjectNumber))
            {
                _logger.LogWarning(
                    "ProjectService returned an invalid ownership payload for project {ProjectId}.",
                    projectId);
                return new ProjectOwnershipResult(ProjectOwnershipStatus.Unavailable);
            }

            return project.CustomerId == customerId
                ? new ProjectOwnershipResult(ProjectOwnershipStatus.Owned, project.ProjectNumber.Trim())
                : new ProjectOwnershipResult(ProjectOwnershipStatus.NotOwned);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "ProjectService ownership lookup timed out for project {ProjectId}.",
                projectId);
            return new ProjectOwnershipResult(ProjectOwnershipStatus.Unavailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "ProjectService ownership lookup failed for project {ProjectId}.",
                projectId);
            return new ProjectOwnershipResult(ProjectOwnershipStatus.Unavailable);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "ProjectService returned malformed ownership data for project {ProjectId}.",
                projectId);
            return new ProjectOwnershipResult(ProjectOwnershipStatus.Unavailable);
        }
    }

    private sealed class ProjectOwnershipPayload
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("customerId")]
        public Guid CustomerId { get; init; }

        [JsonPropertyName("projectNumber")]
        public string? ProjectNumber { get; init; }
    }
}
