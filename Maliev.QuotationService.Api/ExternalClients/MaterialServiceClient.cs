using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

/// <summary>
/// Client for interacting with the Material Service API.
/// </summary>
public class MaterialServiceClient : IMaterialServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MaterialServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the MaterialServiceClient.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public MaterialServiceClient(
        HttpClient httpClient,
        ILogger<MaterialServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets a material by its unique identifier.
    /// </summary>
    /// <param name="materialId">The unique identifier of the material.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The material details if found, otherwise null.</returns>
    public async Task<MaterialDto?> GetMaterialByIdAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/material/v1/materials/{materialId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MaterialDto>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get material {MaterialId}", materialId);
            throw;
        }
    }

    /// <summary>
    /// Gets the supported manufacturing processes for a material.
    /// </summary>
    /// <param name="materialId">The unique identifier of the material.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of supported process names.</returns>
    public async Task<IEnumerable<string>> GetSupportedProcessesAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/materials/{materialId}/processes", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ProcessesResponse>(cancellationToken);
            return result?.Processes ?? new List<string>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get supported processes for material {MaterialId}", materialId);
            throw;
        }
    }

    private record ProcessesResponse(List<string> Processes);
}
