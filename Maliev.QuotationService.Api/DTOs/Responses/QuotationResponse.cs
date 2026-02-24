using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.DTOs.Responses;

public class QuotationResponse
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public CustomerDto? Customer { get; set; }
    public Guid? SourceRfqId { get; set; }
    public int CurrentVersionNumber { get; set; }
    public QuotationStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTime ValidityPeriodStart { get; set; }
    public DateTime ValidityPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
