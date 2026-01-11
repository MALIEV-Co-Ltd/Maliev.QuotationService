using System.Net.Http.Headers;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

public class PdfServiceClient : IPdfServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PdfServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PdfServiceClient(
        HttpClient httpClient,
        ILogger<PdfServiceClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PdfGenerationResponseDto> GeneratePdfAsync(QuotationPdfPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
            // Forward authorization header from the current request
            var token = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
            }

            var response = await _httpClient.PostAsJsonAsync("/api/v1/pdf/generate", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PdfGenerationResponseDto>(cancellationToken);
            return result ?? throw new InvalidOperationException("Invalid response from PDF Service");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for quotation {QuotationId}", payload.QuotationId);
            throw;
        }
    }
}
