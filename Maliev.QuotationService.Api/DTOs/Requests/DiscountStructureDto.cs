using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class DiscountStructureDto
{
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public string? Conditions { get; set; }
    public string? AuthorizationReason { get; set; }
}
