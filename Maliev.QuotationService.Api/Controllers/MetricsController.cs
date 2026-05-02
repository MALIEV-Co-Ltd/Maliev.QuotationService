using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.QuotationService.Application.Authorization;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Controllers.v1;

/// <summary>
/// Lightweight business metrics for dashboards.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("quotation/v{version:apiVersion}/metrics")]
[RequirePermission(QuotationPermissions.QuotationsRead)]
public class MetricsController : ControllerBase
{
    private readonly IQuotationService _quotationService;
    private readonly QuotationDbContext _context;
    private readonly ILogger<MetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="MetricsController"/>.
    /// </summary>
    public MetricsController(
        IQuotationService quotationService,
        QuotationDbContext context,
        ILogger<MetricsController> logger)
    {
        _quotationService = quotationService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get the count of pending quotations.
    /// </summary>
    [HttpGet("pending-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingCount(CancellationToken cancellationToken)
    {
        // Use GetAllAsync with status filter to count pending quotations
        var (quotations, totalCount) = await _quotationService.GetAllAsync(
            status: QuotationStatus.PendingApproval,
            page: 1,
            pageSize: 1, // We only need the totalCount
            cancellationToken: cancellationToken);

        return Ok(new { count = totalCount });
    }

    /// <summary>
    /// Get the count of quotations awaiting customer response past the configured age threshold.
    /// </summary>
    /// <param name="minAgeDays">Minimum age in days before a customer-review quotation is considered aging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("aging-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAgingCount(
        [FromQuery] int minAgeDays = 7,
        CancellationToken cancellationToken = default)
    {
        var effectiveMinAgeDays = Math.Max(0, minAgeDays);
        var cutoff = DateTime.UtcNow.AddDays(-effectiveMinAgeDays);

        var count = await _context.Quotations
            .AsNoTracking()
            .CountAsync(
                q => q.Status == QuotationStatus.CustomerReview
                    && !q.IsDeleted
                    && q.UpdatedAt <= cutoff,
                cancellationToken);

        return Ok(new { count });
    }
}
