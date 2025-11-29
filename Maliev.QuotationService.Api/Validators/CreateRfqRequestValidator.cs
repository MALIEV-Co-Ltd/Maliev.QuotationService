using FluentValidation;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.Validators;

public class CreateRfqRequestValidator : AbstractValidator<CreateRfqRequest>
{
    public CreateRfqRequestValidator()
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required")
            .EmailAddress().WithMessage("Customer email must be a valid email address");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required")
            .Length(1, 200).WithMessage("Customer name must be between 1 and 200 characters");

        RuleFor(x => x.ChannelSource)
            .IsInEnum().WithMessage("Channel source must be a valid value");

        When(x => x.CustomerPhoneNumber != null, () =>
        {
            RuleFor(x => x.CustomerPhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Customer phone number must be a valid international phone number");
        });
    }
}
