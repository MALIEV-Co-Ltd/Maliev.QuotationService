using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.QuotationService.Api.Services.IAM;
using Maliev.QuotationService.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Maliev.QuotationService.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("quotation/v{version:apiVersion}/customers")]
[RequirePermission(QuotationPermissions.QuotationsRead)]
public class CustomerController : ControllerBase
{
    private readonly ICustomerMatchingService _matchingService;
    private readonly ILogger<CustomerController> _logger;

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

public class GetCustomerMatchesRequest
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Name { get; set; }
}

public class LinkCustomerRequest
{
    public Guid SourceCustomerId { get; set; }
    public Guid TargetCustomerId { get; set; }
}
