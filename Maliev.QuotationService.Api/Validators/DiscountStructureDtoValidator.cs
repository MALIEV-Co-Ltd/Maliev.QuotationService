using FluentValidation;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.Validators;

public class DiscountStructureDtoValidator : AbstractValidator<DiscountStructureDto>
{
    public DiscountStructureDtoValidator()
    {
        RuleFor(x => x.DiscountType)
            .IsInEnum().WithMessage("Discount type must be a valid value");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0")
            .LessThanOrEqualTo(100).When(x => x.DiscountType == DiscountType.Percentage)
            .WithMessage("Percentage discount cannot exceed 100%");

        When(x => !string.IsNullOrEmpty(x.Conditions), () =>
        {
            RuleFor(x => x.Conditions)
                .MaximumLength(500).WithMessage("Conditions cannot exceed 500 characters");
        });

        When(x => !string.IsNullOrEmpty(x.AuthorizationReason), () =>
        {
            RuleFor(x => x.AuthorizationReason)
                .MaximumLength(500).WithMessage("Authorization reason cannot exceed 500 characters");
        });
    }
}
