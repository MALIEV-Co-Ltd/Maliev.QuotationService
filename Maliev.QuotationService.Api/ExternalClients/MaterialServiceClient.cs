using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

public class MaterialServiceClient : IMaterialServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MaterialServiceClient> _logger;

    public MaterialServiceClient(HttpClient httpClient, ILogger<MaterialServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MaterialDto?> GetMaterialByIdAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/materials/v1/Materials/{materialId}", cancellationToken);

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

    public async Task<IEnumerable<string>> GetSupportedProcessesAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/materials/v1/Materials/{materialId}/processes", cancellationToken);
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
