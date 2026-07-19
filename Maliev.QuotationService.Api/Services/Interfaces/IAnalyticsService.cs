using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Api.Services.Interfaces;

/// <summary>
/// Service for business performance analytics and reporting.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets RFQ to quotation conversion rates by channel.
    /// </summary>
    Task<IEnumerable<ConversionRateReport>> GetConversionRatesByChannelAsync(
        RfqChannel? channelSource = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets average turnaround time from RFQ to quotation.
    /// </summary>
    Task<TurnaroundTimeReport> GetAverageTurnaroundTimeAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of abandoned RFQs (not converted within threshold).
    /// </summary>
    Task<IEnumerable<AbandonedRfqReport>> GetAbandonedRfqsAsync(
        int thresholdDays = 30,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Report model for RFQ to quotation conversion rates.
/// </summary>
public class ConversionRateReport
{
    /// <summary>
    /// The channel source for the RFQs.
    /// </summary>
    public RfqChannel ChannelSource { get; set; }

    /// <summary>
    /// The total number of RFQs.
    /// </summary>
    public int TotalRfqs { get; set; }

    /// <summary>
    /// The number of RFQs converted to quotations.
    /// </summary>
    public int ConvertedToQuotations { get; set; }

    /// <summary>
    /// The conversion rate as a percentage.
    /// </summary>
    public double ConversionRate => TotalRfqs > 0 ? (double)ConvertedToQuotations / TotalRfqs * 100 : 0;
}

/// <summary>
/// Report model for average turnaround time from RFQ to quotation.
/// </summary>
public class TurnaroundTimeReport
{
    /// <summary>
    /// The average number of days from RFQ to quotation.
    /// </summary>
    public double AverageDays { get; set; }

    /// <summary>
    /// The median number of days from RFQ to quotation.
    /// </summary>
    public double MedianDays { get; set; }

    /// <summary>
    /// The minimum number of days from RFQ to quotation.
    /// </summary>
    public double MinDays { get; set; }

    /// <summary>
    /// The maximum number of days from RFQ to quotation.
    /// </summary>
    public double MaxDays { get; set; }
}

/// <summary>
/// Report model for abandoned RFQs.
/// </summary>
public class AbandonedRfqReport
{
    /// <summary>
    /// The unique identifier of the abandoned RFQ.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the customer.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// The channel source of the RFQ.
    /// </summary>
    public RfqChannel ChannelSource { get; set; }

    /// <summary>
    /// The timestamp when the RFQ was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The number of days since the RFQ was created.
    /// </summary>
    public int DaysSinceCreation => (DateTime.UtcNow - CreatedAt).Days;
}
