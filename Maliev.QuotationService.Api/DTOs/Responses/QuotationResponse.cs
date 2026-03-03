using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Api.DTOs.Responses;

public class QuotationResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public CustomerDto? Customer { get; set; }
    public Guid? SourceRfqId { get; set; }
    public int CurrentVersionNumber { get; set; }
    public QuotationStatus Status { get; set; }
    public DateTime ValidityPeriodStart { get; set; }
    public DateTime ValidityPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
