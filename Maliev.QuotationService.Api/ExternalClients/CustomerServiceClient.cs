using System.Text.Json;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Api.ExternalClients;

/// <summary>
/// HTTP client for interacting with the Customer Service API
/// </summary>
public class CustomerServiceClient : ICustomerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the CustomerServiceClient
    /// </summary>
    public CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<CustomerApiResponse?> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/customer/v1/customers/{customerId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Customer {CustomerId} not found in Customer Service", customerId);
                    return null;
                }

                _logger.LogError("Failed to fetch customer {CustomerId}: {StatusCode}", customerId, response.StatusCode);
                throw new HttpRequestException(
                    $"Customer Service returned {(int)response.StatusCode} {response.StatusCode} while fetching customer {customerId}.",
                    null,
                    response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var customer = JsonSerializer.Deserialize<CustomerApiResponse>(content, _jsonOptions);

            return customer;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Timed out while fetching customer {CustomerId}", customerId);
            throw new TimeoutException($"Timed out while fetching customer {customerId} from Customer Service.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Customer fetch for {CustomerId} was cancelled", customerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching customer {CustomerId}", customerId);
            throw;
        }
    }
}
