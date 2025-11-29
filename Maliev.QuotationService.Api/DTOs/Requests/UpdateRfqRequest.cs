namespace Maliev.QuotationService.Api.DTOs.Requests;

public class UpdateRfqRequest
{
    public object? RequestDetails { get; set; }
    public string? AssignedStaffUserId { get; set; }
}
