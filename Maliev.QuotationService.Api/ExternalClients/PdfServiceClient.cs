using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

/// <summary>
/// Client for interacting with the PDF Service API.
/// </summary>
public class PdfServiceClient : IPdfServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PdfServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the PdfServiceClient.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public PdfServiceClient(
        HttpClient httpClient,
        ILogger<PdfServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Generates a PDF for a quotation.
    /// </summary>
    /// <param name="payload">The quotation data to include in the PDF.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PDF generation response.</returns>
    public async Task<PdfGenerationResponseDto> GeneratePdfAsync(QuotationPdfPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
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
