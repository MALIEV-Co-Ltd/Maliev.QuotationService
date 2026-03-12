using Maliev.QuotationService.Domain.Enums;
using System.Text.Json;

namespace Maliev.QuotationService.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public AuditEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AuditActionType ActionType { get; set; }
    public DateTime Timestamp { get; set; }
    public JsonDocument? ChangedFields { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
