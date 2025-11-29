namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

public interface ICurrencyServiceClient
{
    Task<decimal> GetConversionRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAvailableCurrenciesAsync(CancellationToken cancellationToken = default);
}
