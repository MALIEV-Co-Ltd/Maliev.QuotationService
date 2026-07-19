namespace Maliev.QuotationService.Domain.Entities;

public class StaffRole
{
    public Guid Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new List<string>();
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
