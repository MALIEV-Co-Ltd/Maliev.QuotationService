using Asp.Versioning;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Data;
using Maliev.QuotationService.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Maliev.QuotationService.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("quotation/v{version:apiVersion}/rfqs")]
[Authorize(Policy = "EmployeeOrHigher")]
public class RfqController : ControllerBase
{
    private readonly IRfqService _rfqService;
    private readonly QuotationDbContext _context;
    private readonly ILogger<RfqController> _logger;

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
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RfqResponse>> CreateRfq(
        [FromBody] CreateRfqRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        // Find or create customer
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == request.CustomerEmail, cancellationToken);

        if (customer == null)
        {
            customer = new Data.Entities.Customer
            {
                Id = Guid.NewGuid(),
                Email = request.CustomerEmail,
                Name = request.CustomerName,
                PhoneNumber = request.CustomerPhoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new customer {CustomerId} with email {Email}", customer.Id, customer.Email);
        }

        // Create RFQ
        var rfq = await _rfqService.CreateAsync(
            customerId: customer.Id,
            channelSource: request.ChannelSource,
            requestDetails: request.RequestDetails,
            uploadServiceFileIds: request.UploadServiceFileIds,
            currentUserId: currentUserId,
            cancellationToken: cancellationToken);

        var response = MapToResponse(rfq, customer);

        return CreatedAtAction(nameof(GetRfqById), new { id = rfq.Id }, response);
    }

    /// <summary>
    /// Get all RFQs with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RfqResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RfqResponse>>> GetRfqs(
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

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(responses);
    }

    /// <summary>
    /// Get RFQ by ID
    /// </summary>
    [HttpGet("{id}")]
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

        var response = MapToResponse(rfq, rfq.Customer);

        return Ok(response);
    }

    /// <summary>
    /// Update RFQ details
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqResponse>> UpdateRfq(
        Guid id,
        [FromBody] UpdateRfqRequest request,
        CancellationToken cancellationToken)
    {
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
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqResponse>> UpdateRfqStatus(
        Guid id,
        [FromBody] UpdateRfqStatusRequest request,
        CancellationToken cancellationToken)
    {
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
    [ProducesResponseType(typeof(InternalNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InternalNoteResponse>> AddNote(
        Guid id,
        [FromBody] AddInternalNoteRequest request,
        CancellationToken cancellationToken)
    {
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
    [Authorize(Policy = "Employee")]
    [ProducesResponseType(typeof(RfqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqResponse>> AssignRfq(
        Guid id,
        [FromBody] AssignRfqRequest request,
        CancellationToken cancellationToken)
    {
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

    private static RfqResponse MapToResponse(Data.Entities.Rfq rfq, Data.Entities.Customer customer)
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
}
