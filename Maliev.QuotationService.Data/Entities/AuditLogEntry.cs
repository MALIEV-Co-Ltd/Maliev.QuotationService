using System.Text.Json;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Data.Entities;

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
