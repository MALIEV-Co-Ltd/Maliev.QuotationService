using System.Security.Claims;

namespace Maliev.QuotationService.Api.Authorization;

internal static class CustomerClaimScope
{
    public static bool TryGetCustomerId(ClaimsPrincipal user, out Guid customerId)
    {
        var rawCustomerId = user.FindFirst("customer_id")?.Value
            ?? user.FindFirst("customerId")?.Value;

        return Guid.TryParse(rawCustomerId, out customerId);
    }
}
