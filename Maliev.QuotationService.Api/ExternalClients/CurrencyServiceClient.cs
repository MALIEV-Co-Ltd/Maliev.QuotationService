using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

/// <summary>
/// Client for interacting with the Currency Service API.
/// </summary>
public class CurrencyServiceClient : ICurrencyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the CurrencyServiceClient.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public CurrencyServiceClient(
        HttpClient httpClient,
        ILogger<CurrencyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets the conversion rate between two currencies.
    /// </summary>
    /// <param name="fromCurrency">The source currency code.</param>
    /// <param name="toCurrency">The target currency code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The conversion rate.</returns>
    public async Task<decimal> GetConversionRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/rates/{fromCurrency}/{toCurrency}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ConversionRateResponse>(cancellationToken);
            return result?.Rate ?? throw new InvalidOperationException("Invalid response from Currency Service");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get conversion rate from {From} to {To}", fromCurrency, toCurrency);
            throw;
        }
    }

    /// <summary>
    /// Gets the list of available currencies.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of currency codes.</returns>
    public async Task<IEnumerable<string>> GetAvailableCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/currencies", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CurrenciesResponse>(cancellationToken);
            return result?.Currencies ?? new List<string>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get available currencies");
            throw;
        }
    }

    private record ConversionRateResponse(decimal Rate);
    private record CurrenciesResponse(List<string> Currencies);
}
