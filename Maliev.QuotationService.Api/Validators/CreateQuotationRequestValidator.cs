using FluentValidation;
using Maliev.QuotationService.Api.DTOs.Requests;

namespace Maliev.QuotationService.Api.Validators;

public class CreateQuotationRequestValidator : AbstractValidator<CreateQuotationRequest>
{
    public CreateQuotationRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.ValidityPeriodEnd)
            .GreaterThan(x => x.ValidityPeriodStart)
            .WithMessage("Validity period end must be after validity period start");

        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("At least one line item is required")
            .Must(items => items.Count > 0).WithMessage("Line items cannot be empty");

        RuleForEach(x => x.LineItems)
            .SetValidator(new QuotationLineItemDtoValidator());

        When(x => x.DiscountStructure != null, () =>
        {
            RuleFor(x => x.DiscountStructure)
                .SetValidator(new DiscountStructureDtoValidator()!);
        });
    }
}
