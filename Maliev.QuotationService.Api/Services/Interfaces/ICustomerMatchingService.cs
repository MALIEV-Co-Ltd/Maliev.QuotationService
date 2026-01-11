using Maliev.QuotationService.Data.Entities;

namespace Maliev.QuotationService.Api.Services.Interfaces;

/// <summary>
/// Service for matching and linking customer records across channels.
/// </summary>
public interface ICustomerMatchingService
{
    /// <summary>
    /// Gets potential customer matches based on contact information.
    /// </summary>
    Task<IEnumerable<CustomerMatch>> GetMatchSuggestionsAsync(
        string? email,
        string? phoneNumber,
        string? name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a source customer to a target customer (merges records).
    /// </summary>
    Task LinkCustomersAsync(
        Guid sourceCustomerId,
        Guid targetCustomerId,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlinks a previously merged customer record.
    /// </summary>
    Task UnlinkCustomersAsync(
        Guid sourceCustomerId,
        Guid targetCustomerId,
        string currentUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a potential customer match.
/// </summary>
public class CustomerMatch
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public double MatchConfidence { get; set; }
    public List<string> MatchingFields { get; set; } = new();
}
