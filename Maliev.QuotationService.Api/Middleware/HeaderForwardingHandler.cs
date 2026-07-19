using System.Net.Http.Headers;

namespace Maliev.QuotationService.Api.Middleware;

/// <summary>
/// A delegating handler that forwards the Authorization header from the current HttpContext
/// to outgoing HTTP requests.
/// </summary>
public class HeaderForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the HeaderForwardingHandler.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public HeaderForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Sends the HTTP request with the Authorization header forwarded.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
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
