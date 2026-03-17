using Maliev.QuotationService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for discount structure applied to a quotation.
/// </summary>
public class DiscountStructureDto
{
    /// <summary>
    /// The type of discount to apply.
    /// </summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>
    /// The value of the discount (percentage or fixed amount).
    /// </summary>
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Conditions that must be met for the discount to apply.
    /// </summary>
    [StringLength(500)]
    public string? Conditions { get; set; }

    /// <summary>
    /// The reason this discount was authorized.
    /// </summary>
    [StringLength(500)]
    public string? AuthorizationReason { get; set; }
}
