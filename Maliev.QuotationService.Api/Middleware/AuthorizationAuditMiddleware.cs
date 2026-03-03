using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using System.Security.Claims;

namespace Maliev.QuotationService.Api.Middleware;

/// <summary>
/// Middleware that audits unauthorized access attempts (403 Forbidden).
/// </summary>
public class AuthorizationAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationAuditMiddleware> _logger;

    public AuthorizationAuditMiddleware(RequestDelegate next, ILogger<AuthorizationAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, QuotationDbContext dbContext)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            await AuditUnauthorizedAttemptAsync(context, dbContext);
        }
    }

    private async Task AuditUnauthorizedAttemptAsync(HttpContext context, QuotationDbContext dbContext)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("sub")?.Value
                     ?? "anonymous";

        var path = context.Request.Path;
        var method = context.Request.Method;

        _logger.LogWarning("Unauthorized access attempt by user {UserId} to {Method} {Path}", userId, method, path);

        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.Security,
            EntityId = Guid.Empty, // No specific entity for access denial
            UserId = userId,
            ActionType = AuditActionType.Unauthorized,
            Timestamp = DateTime.UtcNow,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            // Store details about the failed request in ChangedFields
            ChangedFields = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(new { path = path.ToString(), method }))
        };

        try
        {
            dbContext.AuditLogEntries.Add(auditEntry);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist unauthorized access audit log entry.");
        }
    }
}
