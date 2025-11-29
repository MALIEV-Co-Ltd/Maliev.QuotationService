using FluentValidation;
using Maliev.QuotationService.Api.DTOs.Requests;

namespace Maliev.QuotationService.Api.Validators;

public class UpdateQuotationRequestValidator : AbstractValidator<UpdateQuotationRequest>
{
    public UpdateQuotationRequestValidator()
    {
        RuleFor(x => x.ChangeSummary)
            .NotEmpty().WithMessage("Change summary is required")
            .MaximumLength(500).WithMessage("Change summary cannot exceed 500 characters");

        When(x => x.LineItems != null && x.LineItems.Count > 0, () =>
        {
            RuleForEach(x => x.LineItems)
                .SetValidator(new QuotationLineItemDtoValidator());
        });

        When(x => x.DiscountStructure != null, () =>
        {
            RuleFor(x => x.DiscountStructure)
                .SetValidator(new DiscountStructureDtoValidator()!);
        });
    }
}
