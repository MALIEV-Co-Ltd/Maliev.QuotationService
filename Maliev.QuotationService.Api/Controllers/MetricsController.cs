using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.QuotationService.Api.Services.IAM;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ILogger<MetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="MetricsController"/>.
    /// </summary>
    public MetricsController(IQuotationService quotationService, ILogger<MetricsController> logger)
    {
        _quotationService = quotationService;
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
}
