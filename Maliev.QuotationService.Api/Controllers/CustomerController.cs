using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.QuotationService.Api.Authorization;
using Maliev.QuotationService.Application.Authorization;
using Maliev.QuotationService.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Maliev.QuotationService.Api.Controllers.v1;

/// <summary>
/// Controller for customer matching and linking operations.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("quotation/v{version:apiVersion}/customers")]
[RequirePermission(QuotationPermissions.QuotationsRead)]
public class CustomerController : ControllerBase
{
    private readonly ICustomerMatchingService _matchingService;
    private readonly ILogger<CustomerController> _logger;

    /// <summary>
    /// Initializes a new instance of the CustomerController.
    /// </summary>
    /// <param name="matchingService">The customer matching service.</param>
    /// <param name="logger">The logger.</param>
    public CustomerController(
        ICustomerMatchingService matchingService,
        ILogger<CustomerController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets potential customer matches based on contact information.
    /// </summary>
    [HttpPost("match")]
    [RequirePermission(QuotationPermissions.QuotationsRead)]
    public async Task<ActionResult<IEnumerable<CustomerMatch>>> GetMatches(
        [FromBody] GetCustomerMatchesRequest request,
        CancellationToken cancellationToken)
    {
        if (CustomerClaimScope.TryGetCustomerId(User, out _)) return Forbid();

        var matches = await _matchingService.GetMatchSuggestionsAsync(
            request.Email, request.PhoneNumber, request.Name, cancellationToken);

        return Ok(matches);
    }

    /// <summary>
    /// Links a source customer to a target customer (merges records).
    /// </summary>
    [HttpPost("link")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    public async Task<IActionResult> LinkCustomers(
        [FromBody] LinkCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (CustomerClaimScope.TryGetCustomerId(User, out _)) return Forbid();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            await _matchingService.LinkCustomersAsync(
                request.SourceCustomerId, request.TargetCustomerId, currentUserId, cancellationToken);

            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Unlinks a previously merged customer record.
    /// </summary>
    [HttpPost("unlink")]
    [RequirePermission(QuotationPermissions.QuotationsUpdate)]
    public async Task<IActionResult> UnlinkCustomers(
        [FromBody] LinkCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (CustomerClaimScope.TryGetCustomerId(User, out _)) return Forbid();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            await _matchingService.UnlinkCustomersAsync(
                request.SourceCustomerId, request.TargetCustomerId, currentUserId, cancellationToken);

            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Request model for retrieving customer matches based on contact information.
/// </summary>
public class GetCustomerMatchesRequest
{
    /// <summary>
    /// The customer's email address.
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// The customer's phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }
    /// <summary>
    /// The customer's name.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Request model for linking two customer records together.
/// </summary>
public class LinkCustomerRequest
{
    /// <summary>
    /// The unique identifier of the source customer to be merged.
    /// </summary>
    public Guid SourceCustomerId { get; set; }
    /// <summary>
    /// The unique identifier of the target customer to merge into.
    /// </summary>
    public Guid TargetCustomerId { get; set; }
}
