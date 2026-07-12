using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.QuotationService.Api.Authorization;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Api.Exceptions;
using Maliev.QuotationService.Application.Authorization;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Maliev.QuotationService.Api.Controllers.v1;

/// <summary>
/// Controller for managing Request for Quotation (RFQ) operations including creation, retrieval, and status updates.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("quotation/v{version:apiVersion}/rfqs")]
[RequirePermission(QuotationPermissions.QuotationsRead)]
public class RfqController : ControllerBase
{
    private readonly IRfqService _rfqService;
    private readonly QuotationDbContext _context;
    private readonly ILogger<RfqController> _logger;

    /// <summary>
    /// Initializes a new instance of the RfqController.
    /// </summary>
    /// <param name="rfqService">The RFQ service.</param>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public RfqController(
        IRfqService rfqService,
        QuotationDbContext context,
        ILogger<RfqController> logger)
    {
        _rfqService = rfqService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new RFQ
    /// </summary>
    [HttpPost]
    [RequirePermission(QuotationPermissions.QuotationsCreate)]
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RfqResponse>> CreateRfq(
        [FromBody] CreateRfqRequest request,
        CancellationToken cancellationToken)
    {
        if (CustomerClaimScope.TryGetCustomerId(User, out _)) return Forbid();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        // RFQ and Customer creation is now atomic within the service
        var rfq = await _rfqService.CreateAsync(
            customerEmail: request.CustomerEmail,
            customerName: request.CustomerName,
            customerPhoneNumber: request.CustomerPhoneNumber,
            channelSource: request.ChannelSource,
            requestDetails: request.RequestDetails,
            uploadServiceFileIds: request.UploadServiceFileIds,
            currentUserId: currentUserId,
            cancellationToken: cancellationToken);

        // Reload to get customer data
        var fullRfq = await _rfqService.GetByIdAsync(rfq.Id, cancellationToken);
        var response = MapToResponse(fullRfq!, fullRfq!.Customer);

        return CreatedAtAction(nameof(GetRfqById), new { id = rfq.Id }, response);
    }

    /// <summary>
    /// Get all RFQs with optional filtering
    /// </summary>
    [HttpGet]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    [ProducesResponseType(typeof(PagedResponse<RfqResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RfqResponse>>> GetRfqs(
        [FromQuery] RfqChannel? channel = null,
        [FromQuery] RfqStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? assignedStaffUserId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (CustomerClaimScope.TryGetCustomerId(User, out var scopedCustomerId))
        {
            if (customerId.HasValue && customerId.Value != scopedCustomerId)
            {
                return Forbid();
            }

            customerId = scopedCustomerId;
        }

        var (rfqs, totalCount) = await _rfqService.GetAllAsync(
            channelSource: channel,
            status: status,
            customerId: customerId,
            assignedStaffUserId: assignedStaffUserId,
            fromDate: fromDate,
            toDate: toDate,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var responses = rfqs.Select(r => MapToResponse(r, r.Customer)).ToList();

        return Ok(new PagedResponse<RfqResponse>
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
    /// Get RFQ by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqResponse>> GetRfqById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var rfq = await _rfqService.GetByIdAsync(id, cancellationToken);

        if (rfq == null)
        {
            return NotFound(new { message = $"RFQ with ID {id} not found" });
        }
        if (IsOutsideCustomerScope(rfq.CustomerId)) return Forbid();

        var response = MapToResponse(rfq, rfq.Customer);

        return Ok(response);
    }

    /// <summary>
    /// Update RFQ details
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqResponse>> UpdateRfq(
        Guid id,
        [FromBody] UpdateRfqRequest request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await EnsureRfqInCustomerScopeAsync(id, cancellationToken);
        if (scopeResult is not null) return scopeResult;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            var rfq = await _rfqService.UpdateAsync(
                rfqId: id,
                requestDetails: request.RequestDetails,
                assignedStaffUserId: request.AssignedStaffUserId,
                currentUserId: currentUserId,
                cancellationToken: cancellationToken);

            // Reload with customer
            var updated = await _rfqService.GetByIdAsync(id, cancellationToken);
            var response = MapToResponse(updated!, updated!.Customer);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update RFQ status
    /// </summary>
    [HttpPatch("{id}/status")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqResponse>> UpdateRfqStatus(
        Guid id,
        [FromBody] UpdateRfqStatusRequest request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await EnsureRfqInCustomerScopeAsync(id, cancellationToken);
        if (scopeResult is not null) return scopeResult;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            var rfq = await _rfqService.UpdateStatusAsync(
                rfqId: id,
                status: request.Status,
                currentUserId: currentUserId,
                cancellationToken: cancellationToken);

            // Reload with customer
            var updated = await _rfqService.GetByIdAsync(id, cancellationToken);
            var response = MapToResponse(updated!, updated!.Customer);

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
    /// Add internal note to RFQ
    /// </summary>
    [HttpPost("{id}/notes")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    [ProducesResponseType(typeof(InternalNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InternalNoteResponse>> AddNote(
        Guid id,
        [FromBody] AddInternalNoteRequest request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await EnsureRfqInCustomerScopeAsync(id, cancellationToken);
        if (scopeResult is not null) return scopeResult;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            var note = await _rfqService.AddNoteAsync(
                rfqId: id,
                content: request.Content,
                currentUserId: currentUserId,
                cancellationToken: cancellationToken);

            var response = new InternalNoteResponse
            {
                Id = note.Id,
                AuthorUserId = note.AuthorUserId,
                Content = note.Content,
                CreatedAt = note.CreatedAt
            };

            return CreatedAtAction(nameof(GetRfqById), new { id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assign RFQ to staff member
    /// </summary>
    [HttpPatch("{id}/assign")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqResponse>> AssignRfq(
        Guid id,
        [FromBody] AssignRfqRequest request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await EnsureRfqInCustomerScopeAsync(id, cancellationToken);
        if (scopeResult is not null) return scopeResult;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            var rfq = await _rfqService.AssignAsync(
                rfqId: id,
                assignedStaffUserId: request.AssignedStaffUserId,
                currentUserId: currentUserId,
                cancellationToken: cancellationToken);

            // Reload with customer
            var updated = await _rfqService.GetByIdAsync(id, cancellationToken);
            var response = MapToResponse(updated!, updated!.Customer);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Marks an RFQ as converted
    /// </summary>
    [HttpPost("{id}/convert")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkAsConverted(
        Guid id,
        CancellationToken cancellationToken)
    {
        var scopeResult = await EnsureRfqInCustomerScopeAsync(id, cancellationToken);
        if (scopeResult is not null) return scopeResult;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            await _rfqService.MarkRfqAsConvertedAsync(id, currentUserId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RfqConversionConflictException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "RFQ cannot be converted.");
        }
    }
    private static RfqResponse MapToResponse(Domain.Entities.Rfq rfq, Domain.Entities.Customer customer)
    {
        return new RfqResponse
        {
            Id = rfq.Id,
            Customer = new CustomerDto
            {
                Id = customer.Id,
                Email = customer.Email,
                Name = customer.Name,
                PhoneNumber = customer.PhoneNumber
            },
            ChannelSource = rfq.ChannelSource,
            Status = rfq.Status,
            RequestDetails = rfq.RequestDetails != null
                ? JsonSerializer.Deserialize<object>(rfq.RequestDetails.RootElement.GetRawText())
                : null,
            AssignedStaffUserId = rfq.AssignedStaffUserId,
            CreatedAt = rfq.CreatedAt,
            UpdatedAt = rfq.UpdatedAt
        };
    }

    private async Task<ActionResult?> EnsureRfqInCustomerScopeAsync(Guid rfqId, CancellationToken cancellationToken)
    {
        if (!CustomerClaimScope.TryGetCustomerId(User, out var scopedCustomerId))
        {
            return null;
        }

        var rfq = await _context.Rfqs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == rfqId, cancellationToken);
        if (rfq is null)
        {
            return NotFound(new { message = $"RFQ with ID {rfqId} not found" });
        }

        return rfq.CustomerId == scopedCustomerId ? null : Forbid();
    }

    private bool IsOutsideCustomerScope(Guid customerId) =>
        CustomerClaimScope.TryGetCustomerId(User, out var scopedCustomerId) && customerId != scopedCustomerId;
}
