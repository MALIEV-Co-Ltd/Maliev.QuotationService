using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

/// <summary>
/// Client for interacting with the Upload Service API.
/// </summary>
public class UploadServiceClient : IUploadServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the UploadServiceClient.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public UploadServiceClient(
        HttpClient httpClient,
        ILogger<UploadServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Validates a file reference and retrieves its metadata.
    /// </summary>
    /// <param name="uploadServiceFileId">The unique identifier of the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file metadata if found, otherwise null.</returns>
    public async Task<FileMetadataDto?> ValidateFileReferenceAsync(Guid uploadServiceFileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/files/{uploadServiceFileId}/metadata", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FileMetadataDto>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to validate file reference {FileId}", uploadServiceFileId);
            throw;
        }
    }
}
