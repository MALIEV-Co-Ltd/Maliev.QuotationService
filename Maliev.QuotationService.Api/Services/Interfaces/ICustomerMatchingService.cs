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
    /// <summary>
    /// The unique identifier of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The customer's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The customer's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The customer's phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The confidence score of the match (0-100).
    /// </summary>
    public double MatchConfidence { get; set; }

    /// <summary>
    /// The list of fields that matched.
    /// </summary>
    public List<string> MatchingFields { get; set; } = new();
}
