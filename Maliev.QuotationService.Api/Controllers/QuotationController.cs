using Asp.Versioning;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Application.Authorization;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Maliev.QuotationService.Api.Controllers;

/// <summary>
/// Controller for managing quotations including creation, retrieval, sending, acceptance, and expiration.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("quotation/v{version:apiVersion}/quotations")]
[RequirePermission(QuotationPermissions.QuotationsRead)]
public class QuotationController : ControllerBase
{
    private readonly IQuotationService _quotationService;
    private readonly QuotationDbContext _context;
    private readonly ILogger<QuotationController> _logger;
    private readonly MetricsService _metricsService;

    /// <summary>
    /// Initializes a new instance of the QuotationController.
    /// </summary>
    /// <param name="quotationService">The quotation service.</param>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="metricsService">The metrics service.</param>
    public QuotationController(
        IQuotationService quotationService,
        QuotationDbContext context,
        ILogger<QuotationController> logger,
        MetricsService metricsService)
    {
        _quotationService = quotationService;
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Create a new quotation.
    /// </summary>
    /// <remarks>
    /// Initiates a new quotation for a customer. Can optionally be linked to a source RFQ.
    /// </remarks>
    /// <param name="request">The quotation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created quotation details.</returns>
    /// <response code="201">Quotation created successfully.</response>
    /// <response code="403">If user lacks `quotation.quotations.create` permission.</response>
    [HttpPost]
    [RequirePermission(QuotationPermissions.QuotationsCreate)]
    [ProducesResponseType(typeof(QuotationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuotationResponse>> CreateQuotation(
        [FromBody] CreateQuotationRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            var quotation = await _quotationService.CreateAsync(
                customerId: request.CustomerId,
                sourceRfqId: request.SourceRfqId,
                validityPeriodStart: request.ValidityPeriodStart,
                validityPeriodEnd: request.ValidityPeriodEnd,
                lineItems: request.LineItems,
                deliveryExpectations: request.DeliveryExpectations,
                currentUserId: currentUserId,
                billingIdentityType: (Domain.Enums.BillingIdentityType)(int)request.BillingIdentityType,
                discountStructure: request.DiscountStructure,
                manualDiscountAmount: request.ManualDiscountAmount,
                shippingCost: request.ShippingCost,
                taxAmount: request.TaxAmount,
                specialTerms: request.SpecialTerms,
                cancellationToken: cancellationToken);

            // Reload with full data
            var fullQuotation = await _quotationService.GetByIdAsync(quotation.Id, cancellationToken);
            var response = await MapToResponseAsync(fullQuotation!, cancellationToken);

            return CreatedAtAction(nameof(GetQuotationById), new { id = quotation.Id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quotation: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get all quotations with optional filtering.
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of quotations. Supports filtering by status, customer, and date range.
    /// </remarks>
    /// <response code="200">List of quotations found.</response>
    /// <response code="403">If user lacks `quotation.quotations.read` permission.</response>
    [HttpGet]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    [ProducesResponseType(typeof(PagedResponse<QuotationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<QuotationResponse>>> GetQuotations(
        [FromQuery] QuotationStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (quotations, totalCount) = await _quotationService.GetAllAsync(
            status: status,
            customerId: customerId,
            fromDate: fromDate,
            toDate: toDate,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var responses = new List<QuotationResponse>();
        foreach (var quotation in quotations)
        {
            responses.Add(await MapToResponseAsync(quotation, cancellationToken));
        }

        return Ok(new PagedResponse<QuotationResponse>
        {
            Data = responses,
            Meta = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        });
    }

    /// <summary>
    /// Get quotation by ID.
    /// </summary>
    /// <remarks>
    /// Fetches full details of a specific quotation.
    /// </remarks>
    /// <response code="200">Quotation found.</response>
    /// <response code="403">If user lacks `quotation.quotations.read` permission.</response>
    /// <response code="404">Quotation not found.</response>
    [HttpGet("{id}")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    [ProducesResponseType(typeof(QuotationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuotationResponse>> GetQuotationById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var quotation = await _quotationService.GetByIdAsync(id, cancellationToken);

        if (quotation == null)
        {
            return NotFound(new { message = $"Quotation with ID {id} not found" });
        }

        var response = await MapToResponseAsync(quotation, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Update quotation.
    /// </summary>
    /// <remarks>
    /// Updates the quotation and automatically creates a new version for history tracking.
    /// </remarks>
    /// <response code="200">Updated successfully.</response>
    /// <response code="403">If user lacks `quotation.quotations.update` permission.</response>
    [HttpPut("{id}")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    [ProducesResponseType(typeof(QuotationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuotationResponse>> UpdateQuotation(
        Guid id,
        [FromBody] UpdateQuotationRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            await _quotationService.UpdateAsync(
                quotationId: id,
                lineItems: request.LineItems,
                changeSummary: request.ChangeSummary,
                  deliveryExpectations: request.DeliveryExpectations,
                  discountStructure: request.DiscountStructure,
                  manualDiscountAmount: request.ManualDiscountAmount,
                  shippingCost: request.ShippingCost,
                  taxAmount: request.TaxAmount,
                  specialTerms: request.SpecialTerms,
                  currentUserId: currentUserId,
                  cancellationToken: cancellationToken);

            // Reload with full data
            var updated = await _quotationService.GetByIdAsync(id, cancellationToken);
            var response = await MapToResponseAsync(updated!, cancellationToken);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Quotation was modified by another user. Please reload and try again." });
        }
    }

    /// <summary>
    /// Update quotation status.
    /// </summary>
    /// <remarks>
    /// Allows changing the status (e.g., to Sent, Accepted, or Rejected).
    /// </remarks>
    /// <response code="200">Status updated.</response>
    /// <response code="403">If user lacks `quotation.quotations.update` permission.</response>
    [HttpPatch("{id}/status")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    [ProducesResponseType(typeof(QuotationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuotationResponse>> UpdateQuotationStatus(
        Guid id,
        [FromBody] UpdateQuotationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            await _quotationService.UpdateStatusAsync(
                quotationId: id,
                status: request.Status,
                currentUserId: currentUserId,
                cancellationToken: cancellationToken);

            // Reload with full data
            var updated = await _quotationService.GetByIdAsync(id, cancellationToken);
            var response = await MapToResponseAsync(updated!, cancellationToken);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Quotation was modified by another user. Please reload and try again." });
        }
    }

    /// <summary>
    /// Approve a quotation.
    /// </summary>
    /// <remarks>
    /// **MANAGER ROLE REQUIRED.** Final approval before sending to customer.
    /// </remarks>
    /// <response code="200">Approved.</response>
    /// <response code="403">If user lacks `quotation.quotations.approve` permission.</response>
    [HttpPost("{id}/approve")]
    [RequirePermission(QuotationPermissions.QuotationsApprove)]
    [ProducesResponseType(typeof(QuotationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuotationResponse>> ApproveQuotation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            await _quotationService.ApproveAsync(
                quotationId: id,
                currentUserId: currentUserId,
                cancellationToken: cancellationToken);

            // Reload with full data
            var updated = await _quotationService.GetByIdAsync(id, cancellationToken);
            var response = await MapToResponseAsync(updated!, cancellationToken);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add an internal note to a quotation.
    /// </summary>
    /// <remarks>
    /// Internal notes are only visible to employees and managers.
    /// </remarks>
    /// <response code="201">Note added.</response>
    /// <response code="403">If user lacks `quotation.quotations.update` permission.</response>
    [HttpPost("{id}/notes")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    [ProducesResponseType(typeof(InternalNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InternalNoteResponse>> AddNoteToQuotation(
        Guid id,
        [FromBody] AddInternalNoteRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            var note = await _quotationService.AddNoteAsync(id, request.Content, currentUserId, cancellationToken);

            var response = new InternalNoteResponse
            {
                Id = note.Id,
                AuthorUserId = note.AuthorUserId,
                Content = note.Content,
                CreatedAt = note.CreatedAt
            };

            return CreatedAtAction(nameof(GetQuotationById), new { id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all versions of a quotation.
    /// </summary>
    /// <remarks>
    /// Useful for auditing changes over time.
    /// </remarks>
    /// <response code="200">Versions found.</response>
    /// <response code="403">If user lacks `quotation.quotations.read` permission.</response>
    [HttpGet("{id}/versions")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    [ProducesResponseType(typeof(List<QuotationVersionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<QuotationVersionResponse>>> GetQuotationVersions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var versions = await _quotationService.GetVersionsAsync(id, cancellationToken);

        if (!versions.Any())
        {
            return NotFound(new { message = $"No versions found for quotation {id}" });
        }

        var responses = versions.Select(MapVersionToResponse).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Get specific version of a quotation.
    /// </summary>
    /// <remarks>
    /// Fetches the immutable state of a quotation at a specific point in time.
    /// </remarks>
    /// <response code="200">Version found.</response>
    /// <response code="403">If user lacks `quotation.quotations.read` permission.</response>
    [HttpGet("{id}/versions/{versionNumber}")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    [ProducesResponseType(typeof(QuotationVersionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuotationVersionResponse>> GetQuotationVersion(
        Guid id,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        var version = await _quotationService.GetVersionByNumberAsync(id, versionNumber, cancellationToken);

        if (version == null)
        {
            return NotFound(new { message = $"Version {versionNumber} not found for quotation {id}" });
        }

        var response = MapVersionToResponse(version);

        return Ok(response);
    }

    /// <summary>
    /// Generate PDF for quotation.
    /// </summary>
    /// <remarks>
    /// Creates a formatted PDF document. If no version is specified, uses the current version.
    /// </remarks>
    /// <response code="200">PDF file returned.</response>
    /// <response code="403">If user lacks `quotation.quotations.read` permission.</response>
    [HttpPost("{id}/pdf")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePdf(
        Guid id,
        [FromQuery] int? versionNumber = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pdfBytes = await _quotationService.GeneratePdfAsync(id, versionNumber, cancellationToken);

            return File(pdfBytes, "application/pdf", $"quotation-{id}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a quotation.
    /// </summary>
    /// <remarks>
    /// Permanently removes a quotation record. Usually restricted to Admin or Manager roles.
    /// </remarks>
    /// <response code="204">Successfully deleted.</response>
    /// <response code="403">If user lacks `quotation.quotations.delete` permission.</response>
    [HttpDelete("{id}")]
    [RequirePermission(QuotationPermissions.QuotationsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuotation(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _quotationService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private async Task<QuotationResponse> MapToResponseAsync(Domain.Entities.Quotation quotation, CancellationToken cancellationToken)
    {
        // Get current version number
        var currentVersion = quotation.Versions.FirstOrDefault(v => v.Id == quotation.CurrentVersionId);
        var versions = quotation.Versions
            .OrderBy(version => version.VersionNumber)
            .Select(MapVersionToResponse)
            .ToList();
        var lineSubtotal = currentVersion?.LineItems.Sum(item => item.LineTotal) ?? 0m;
        var currentDiscount = currentVersion?.DiscountStructures.FirstOrDefault();
        var discountAmount = ResolveDiscountAmount(currentDiscount, lineSubtotal);
        var manualDiscount = currentVersion?.ManualDiscountAmount ?? 0m;
        var shippingCost = currentVersion?.ShippingCost ?? 0m;
        var taxAmount = currentVersion?.TaxAmount ?? 0m;

        return new QuotationResponse
        {
            Id = quotation.Id,
            CustomerId = quotation.CustomerId,
            Customer = quotation.Customer != null ? new CustomerDto
            {
                Id = quotation.Customer.Id,
                Email = quotation.Customer.Email,
                Name = quotation.Customer.Name,
                PhoneNumber = quotation.Customer.PhoneNumber
            } : null,
            SourceRfqId = quotation.SourceRfqId,
            CurrentVersionNumber = currentVersion?.VersionNumber ?? 0,
            Status = quotation.Status,
            QuotationNumber = $"Q-{quotation.Id.ToString("N")[..8].ToUpperInvariant()}",
            ValidityPeriodStart = quotation.ValidityPeriodStart.ToDateTime(TimeOnly.MinValue),
            ValidityPeriodEnd = quotation.ValidityPeriodEnd.ToDateTime(TimeOnly.MinValue),
            SubTotal = Math.Max(0m, lineSubtotal - discountAmount - manualDiscount + shippingCost),
            Tax = taxAmount,
            Total = currentVersion?.TotalPrice ?? 0m,
            CurrencyCode = currentVersion?.CurrencyCode ?? "THB",
            DeliveryExpectations = ParseDeliveryExpectations(currentVersion?.DeliveryExpectations),
            Versions = versions,
            CreatedAt = quotation.CreatedAt,
            UpdatedAt = quotation.UpdatedAt
        };
    }

    private static QuotationVersionResponse MapVersionToResponse(Domain.Entities.QuotationVersion version)
    {
        return new QuotationVersionResponse
        {
            Id = version.Id,
            VersionNumber = version.VersionNumber,
            LineItems = version.LineItems.Select(li => new QuotationLineItemDto
            {
                MaterialServiceId = li.MaterialServiceId,
                Quantity = (int)li.Quantity,
                UnitOfMeasure = li.QuantityUnit,
                UnitPrice = li.UnitPrice,
                ManufacturingProcess = li.ManufacturingProcess,
                Notes = li.Notes
            }).ToList(),
            TotalPrice = version.TotalPrice,
            ManualDiscountAmount = version.ManualDiscountAmount,
            ShippingCost = version.ShippingCost,
            TaxAmount = version.TaxAmount,
            CurrencyCode = version.CurrencyCode,
            DiscountStructure = version.DiscountStructures.FirstOrDefault() != null ? new DiscountStructureDto
            {
                DiscountType = version.DiscountStructures.First().DiscountType,
                DiscountValue = version.DiscountStructures.First().DiscountValue,
                Conditions = version.DiscountStructures.First().Conditions,
                AuthorizationReason = version.DiscountStructures.First().AuthorizationReason
            } : null,
            DeliveryExpectations = ParseDeliveryExpectations(version.DeliveryExpectations),
            ChangeSummary = version.ChangeSummary,
            SpecialTerms = version.SpecialTerms,
            CreatedByUserId = version.CreatedByUserId,
            CreatedAt = version.CreatedAt
        };
    }

    private static string? ParseDeliveryExpectations(JsonDocument? deliveryExpectations)
    {
        if (deliveryExpectations is null)
            return null;

        return deliveryExpectations.RootElement.ValueKind == JsonValueKind.Object &&
               deliveryExpectations.RootElement.TryGetProperty("expectations", out var expectations)
            ? expectations.GetString()
            : deliveryExpectations.RootElement.GetRawText();
    }

    private static decimal ResolveDiscountAmount(Domain.Entities.DiscountStructure? discount, decimal lineSubtotal)
    {
        if (discount is null || discount.DiscountValue <= 0m || lineSubtotal <= 0m)
            return 0m;

        var amount = discount.DiscountType switch
        {
            DiscountType.FixedAmount => discount.DiscountValue,
            DiscountType.Percentage => decimal.Round(lineSubtotal * (discount.DiscountValue / 100m), 2, MidpointRounding.AwayFromZero),
            DiscountType.VolumeBased => decimal.Round(lineSubtotal * (discount.DiscountValue / 100m), 2, MidpointRounding.AwayFromZero),
            _ => 0m
        };

        return Math.Min(Math.Max(0m, amount), lineSubtotal);
    }
}

/// <summary>
/// Request model for updating quotation status.
/// </summary>
public class UpdateQuotationStatusRequest
{
    /// <summary>
    /// The new status to set on the quotation.
    /// </summary>
    public QuotationStatus Status { get; set; }
}
