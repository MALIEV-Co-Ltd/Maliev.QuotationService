using FluentValidation;
using Maliev.QuotationService.Api.DTOs.Requests;

namespace Maliev.QuotationService.Api.Validators;

public class QuotationLineItemDtoValidator : AbstractValidator<QuotationLineItemDto>
{
    public QuotationLineItemDtoValidator()
    {
        RuleFor(x => x.MaterialServiceId)
            .NotEmpty().WithMessage("Material Service ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitOfMeasure)
            .NotEmpty().WithMessage("Unit of measure is required")
            .MaximumLength(50).WithMessage("Unit of measure cannot exceed 50 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be greater than or equal to 0");

        When(x => !string.IsNullOrEmpty(x.ManufacturingProcess), () =>
        {
            RuleFor(x => x.ManufacturingProcess)
                .MaximumLength(200).WithMessage("Manufacturing process cannot exceed 200 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Notes), () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        });
    }
}
