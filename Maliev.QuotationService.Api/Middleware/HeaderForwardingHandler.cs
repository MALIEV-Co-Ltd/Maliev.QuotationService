using System.Net.Http.Headers;

namespace Maliev.QuotationService.Api.Middleware;

/// <summary>
/// A delegating handler that forwards the Authorization header from the current HttpContext
/// to outgoing HTTP requests.
/// </summary>
public class HeaderForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null && context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            if (AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
            {
                request.Headers.Authorization = headerValue;
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
