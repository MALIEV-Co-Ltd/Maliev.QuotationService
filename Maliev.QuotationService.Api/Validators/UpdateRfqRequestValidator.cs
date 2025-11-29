using FluentValidation;
using Maliev.QuotationService.Api.DTOs.Requests;

namespace Maliev.QuotationService.Api.Validators;

public class UpdateRfqRequestValidator : AbstractValidator<UpdateRfqRequest>
{
    public UpdateRfqRequestValidator()
    {
        When(x => x.AssignedStaffUserId != null, () =>
        {
            RuleFor(x => x.AssignedStaffUserId)
                .NotEmpty().WithMessage("Assigned staff user ID cannot be empty if provided");
        });

        // RequestDetails is optional and can be any object
    }
}
