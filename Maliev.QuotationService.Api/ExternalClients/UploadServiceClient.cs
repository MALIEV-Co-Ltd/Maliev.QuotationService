using System.Net.Http.Headers;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

public class UploadServiceClient : IUploadServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UploadServiceClient(
        HttpClient httpClient,
        ILogger<UploadServiceClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<FileMetadataDto?> ValidateFileReferenceAsync(Guid uploadServiceFileId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Forward authorization header from the current request
            var token = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
            }

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
