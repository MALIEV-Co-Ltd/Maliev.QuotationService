using System.Net.Http.Headers;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

public class CurrencyServiceClient : ICurrencyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyServiceClient> _logger;

    public CurrencyServiceClient(
        HttpClient httpClient,
        ILogger<CurrencyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

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
