using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Services;

/// <summary>
/// Service for matching and linking customer records across channels.
/// </summary>
public class CustomerMatchingService : ICustomerMatchingService
{
    private readonly QuotationDbContext _context;
    private readonly ILogger<CustomerMatchingService> _logger;

    /// <summary>
    /// Initializes a new instance of the CustomerMatchingService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public CustomerMatchingService(
        QuotationDbContext context,
        ILogger<CustomerMatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets potential customer matches based on contact information.
    /// </summary>
    /// <param name="email">The customer's email address.</param>
    /// <param name="phoneNumber">The customer's phone number.</param>
    /// <param name="name">The customer's name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of potential customer matches.</returns>
    public async Task<IEnumerable<CustomerMatch>> GetMatchSuggestionsAsync(
        string? email,
        string? phoneNumber,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<CustomerMatch>();

        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phoneNumber) && string.IsNullOrEmpty(name))
        {
            return suggestions;
        }

        // Search for potential candidates using indexed fields where possible
        var query = _context.Customers.AsQueryable();

        var candidates = await _context.Customers
            .Where(c => (email != null && c.Email == email) ||
                        (phoneNumber != null && c.PhoneNumber == phoneNumber) ||
                        (name != null && c.Name.StartsWith(name)))
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var customer in candidates)
        {
            double score = 0;
            var matchingFields = new List<string>();

            if (!string.IsNullOrEmpty(email) && customer.Email == email)
            {
                score += 100;
                matchingFields.Add("Email");
            }

            if (!string.IsNullOrEmpty(phoneNumber) && customer.PhoneNumber == phoneNumber)
            {
                score += 90;
                matchingFields.Add("Phone");
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (customer.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    score += 80;
                    matchingFields.Add("Name (Exact)");
                }
                else if (customer.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    score += 50;
                    matchingFields.Add("Name (Prefix)");
                }
            }

            if (score >= 60)
            {
                suggestions.Add(new CustomerMatch
                {
                    CustomerId = customer.Id,
                    Name = customer.Name,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber,
                    MatchConfidence = Math.Min(score, 100),
                    MatchingFields = matchingFields
                });
            }
        }

        return suggestions.OrderByDescending(s => s.MatchConfidence);
    }

    /// <summary>
    /// Links a source customer to a target customer (merges records).
    /// </summary>
    /// <param name="sourceCustomerId">The source customer ID to be merged.</param>
    /// <param name="targetCustomerId">The target customer ID to merge into.</param>
    /// <param name="currentUserId">The user ID performing the link.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LinkCustomersAsync(
        Guid sourceCustomerId,
        Guid targetCustomerId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (sourceCustomerId == targetCustomerId) return;

        var source = await _context.Customers.FindAsync(new object[] { sourceCustomerId }, cancellationToken);
        var target = await _context.Customers.FindAsync(new object[] { targetCustomerId }, cancellationToken);

        if (source == null || target == null)
        {
            throw new KeyNotFoundException("One or both customers not found");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Move RFQs
            var rfqs = await _context.Rfqs.Where(r => r.CustomerId == sourceCustomerId).ToListAsync(cancellationToken);
            foreach (var rfq in rfqs) rfq.CustomerId = targetCustomerId;

            // Move Quotations
            var quotations = await _context.Quotations.Where(q => q.CustomerId == sourceCustomerId).ToListAsync(cancellationToken);
            foreach (var quotation in quotations) quotation.CustomerId = targetCustomerId;

            // Update merge history
            target.MergedFromIds ??= new List<Guid>();
            target.MergedFromIds.Add(sourceCustomerId);

            source.IsDeleted = true;
            source.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Merged customer {Source} into {Target} by user {User}", sourceCustomerId, targetCustomerId, currentUserId);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Unlinks a previously merged customer record.
    /// </summary>
    /// <param name="sourceCustomerId">The source customer ID to unlink.</param>
    /// <param name="targetCustomerId">The target customer ID.</param>
    /// <param name="currentUserId">The user ID performing the unlink.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task UnlinkCustomersAsync(
        Guid sourceCustomerId,
        Guid targetCustomerId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Simple implementation - in reality this would be more complex
        var source = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == sourceCustomerId, cancellationToken);
        if (source == null) throw new KeyNotFoundException("Source customer not found");

        source.IsDeleted = false;
        source.DeletedAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Unlinked customer {Source} from {Target} by user {User}", sourceCustomerId, targetCustomerId, currentUserId);
    }
}
