using System.ComponentModel.DataAnnotations;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class DiscountStructureDto
{
    public DiscountType DiscountType { get; set; }
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal DiscountValue { get; set; }
    [StringLength(500)]
    public string? Conditions { get; set; }
    [StringLength(500)]
    public string? AuthorizationReason { get; set; }
}
