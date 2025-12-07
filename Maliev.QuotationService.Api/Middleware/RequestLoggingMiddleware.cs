using System.Diagnostics;

namespace Maliev.QuotationService.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Response.Headers.Append("X-Correlation-Id", correlationId);

        var userId = context.User?.Identity?.Name ?? "anonymous";
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
            stopwatch.Stop();

            _logger.LogDebug(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms (User: {UserId}, CorrelationId: {CorrelationId})",
                requestMethod,
                requestPath,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                userId,
                correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "HTTP {Method} {Path} failed with exception in {ElapsedMilliseconds}ms (User: {UserId}, CorrelationId: {CorrelationId})",
                requestMethod,
                requestPath,
                stopwatch.ElapsedMilliseconds,
                userId,
                correlationId);

            throw;
        }
    }
}
