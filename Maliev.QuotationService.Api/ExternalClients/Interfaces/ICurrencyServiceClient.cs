namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

/// <summary>
/// Client interface for interacting with the Currency Service API.
/// </summary>
public interface ICurrencyServiceClient
{
    /// <summary>
    /// Gets the conversion rate between two currencies.
    /// </summary>
    /// <param name="fromCurrency">The source currency code.</param>
    /// <param name="toCurrency">The target currency code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The conversion rate.</returns>
    Task<decimal> GetConversionRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available currencies.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of currency codes.</returns>
    Task<IEnumerable<string>> GetAvailableCurrenciesAsync(CancellationToken cancellationToken = default);
}
