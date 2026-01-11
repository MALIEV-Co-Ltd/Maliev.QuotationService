using Maliev.QuotationService.Data.Enums;

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

public class ConversionRateReport
{
    public RfqChannel ChannelSource { get; set; }
    public int TotalRfqs { get; set; }
    public int ConvertedToQuotations { get; set; }
    public double ConversionRate => TotalRfqs > 0 ? (double)ConvertedToQuotations / TotalRfqs * 100 : 0;
}

public class TurnaroundTimeReport
{
    public double AverageDays { get; set; }
    public double MedianDays { get; set; }
    public double MinDays { get; set; }
    public double MaxDays { get; set; }
}

public class AbandonedRfqReport
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public RfqChannel ChannelSource { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DaysSinceCreation => (DateTime.UtcNow - CreatedAt).Days;
}
