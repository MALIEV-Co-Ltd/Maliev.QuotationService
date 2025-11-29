using System.Text.Json;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Data.Entities;

public class MaterialReference
{
    public Guid Id { get; set; }
    public Guid MaterialServiceId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string? MaterialCategory { get; set; }
    public JsonDocument? PhysicalProperties { get; set; }
    public JsonDocument? MechanicalProperties { get; set; }
    public List<string>? SupportedProcesses { get; set; }
    public MaterialAvailabilityStatus AvailabilityStatus { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
