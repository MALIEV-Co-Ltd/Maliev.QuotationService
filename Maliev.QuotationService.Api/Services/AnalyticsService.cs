using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Services;

/// <summary>
/// Service for business performance analytics and reporting.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly QuotationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    /// <summary>
    /// Initializes a new instance of the AnalyticsService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public AnalyticsService(
        QuotationDbContext context,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets RFQ to quotation conversion rates by channel.
    /// </summary>
    /// <param name="channelSource">Optional channel to filter by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of conversion rate reports.</returns>
    public async Task<IEnumerable<ConversionRateReport>> GetConversionRatesByChannelAsync(
        RfqChannel? channelSource = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Rfqs.AsQueryable();

        if (channelSource.HasValue)
        {
            query = query.Where(r => r.ChannelSource == channelSource.Value);
        }

        var results = await query
            .GroupBy(r => r.ChannelSource)
            .Select(g => new ConversionRateReport
            {
                ChannelSource = g.Key,
                TotalRfqs = g.Count(),
                ConvertedToQuotations = g.Count(r => r.Status == RfqStatus.Converted || r.ConvertedToQuotationId != null)
            })
            .ToListAsync(cancellationToken);

        return results;
    }

    /// <summary>
    /// Gets average turnaround time from RFQ to quotation.
    /// </summary>
    /// <param name="fromDate">Optional start date for filtering.</param>
    /// <param name="toDate">Optional end date for filtering.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The turnaround time report.</returns>
    public async Task<TurnaroundTimeReport> GetAverageTurnaroundTimeAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Quotations
            .Where(q => q.SourceRfqId != null);

        if (fromDate.HasValue)
        {
            query = query.Where(q => q.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(q => q.CreatedAt <= toDate.Value);
        }

        // Fetching data to calculate in memory due to EF Core limitations with date differences in some providers,
        // but normally would use database functions.
        var conversionTimes = await query
            .Select(q => new { q.CreatedAt, RfqCreatedAt = q.SourceRfq!.CreatedAt })
            .ToListAsync(cancellationToken);

        if (!conversionTimes.Any())
        {
            return new TurnaroundTimeReport();
        }

        var durations = conversionTimes.Select(x => (x.CreatedAt - x.RfqCreatedAt).TotalDays).OrderBy(d => d).ToList();
        double median;
        int count = durations.Count;
        if (count % 2 == 0)
        {
            median = (durations[count / 2 - 1] + durations[count / 2]) / 2;
        }
        else
        {
            median = durations[count / 2];
        }

        return new TurnaroundTimeReport
        {
            AverageDays = durations.Average(),
            MinDays = durations.Min(),
            MaxDays = durations.Max(),
            MedianDays = median
        };
    }

    /// <summary>
    /// Gets a list of abandoned RFQs (not converted within threshold).
    /// </summary>
    /// <param name="thresholdDays">Number of days after which an RFQ is considered abandoned.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of abandoned RFQ reports.</returns>
    public async Task<IEnumerable<AbandonedRfqReport>> GetAbandonedRfqsAsync(
        int thresholdDays = 30,
        CancellationToken cancellationToken = default)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(-thresholdDays);

        var abandonedRfqs = await _context.Rfqs
            .Include(r => r.Customer)
            .Where(r => r.Status != RfqStatus.Converted && r.Status != RfqStatus.Abandoned && r.CreatedAt < thresholdDate)
            .Select(r => new AbandonedRfqReport
            {
                Id = r.Id,
                CustomerName = r.Customer.Name,
                ChannelSource = r.ChannelSource,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return abandonedRfqs;
    }
}
