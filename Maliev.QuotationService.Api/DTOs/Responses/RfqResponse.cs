using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.DTOs.Responses;

public class RfqResponse
{
    public Guid Id { get; set; }
    public CustomerDto Customer { get; set; } = new();
    public RfqChannel ChannelSource { get; set; }
    public RfqStatus Status { get; set; }
    public object? RequestDetails { get; set; }
    public string? AssignedStaffUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
