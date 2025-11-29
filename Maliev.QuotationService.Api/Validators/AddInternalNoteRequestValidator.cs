using FluentValidation;
using Maliev.QuotationService.Api.DTOs.Requests;

namespace Maliev.QuotationService.Api.Validators;

public class AddInternalNoteRequestValidator : AbstractValidator<AddInternalNoteRequest>
{
    public AddInternalNoteRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Note content is required")
            .MaximumLength(2000).WithMessage("Note content cannot exceed 2000 characters");
    }
}
