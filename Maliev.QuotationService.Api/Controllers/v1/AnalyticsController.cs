using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults;
using Maliev.QuotationService.Api.Services.IAM;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.QuotationService.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("quotation/v{version:apiVersion}/analytics")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Batch)]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets RFQ to quotation conversion rates by channel.
    /// </summary>
    [HttpGet("conversion-rates")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    public async Task<ActionResult<IEnumerable<ConversionRateReport>>> GetConversionRates(
        [FromQuery] RfqChannel? channel = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _analyticsService.GetConversionRatesByChannelAsync(channel, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Gets average turnaround time from RFQ to quotation.
    /// </summary>
    [HttpGet("turnaround-time")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    public async Task<ActionResult<TurnaroundTimeReport>> GetTurnaroundTime(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _analyticsService.GetAverageTurnaroundTimeAsync(fromDate, toDate, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Gets a list of abandoned RFQs (not converted within threshold).
    /// </summary>
    [HttpGet("abandoned-rfqs")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    public async Task<ActionResult<IEnumerable<AbandonedRfqReport>>> GetAbandonedRfqs(
        [FromQuery] int thresholdDays = 30,
        CancellationToken cancellationToken = default)
    {
        var report = await _analyticsService.GetAbandonedRfqsAsync(thresholdDays, cancellationToken);
        return Ok(report);
    }
}
