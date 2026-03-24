using FluentValidation;
using GdeOni.Application.Deceased.Create.Model;

namespace GdeOni.Application.Deceased.Create.Validation;

public sealed class CreateDeceasedMemoryDtoValidator
    : AbstractValidator<CreateDeceasedMemoryDto>
{
    public CreateDeceasedMemoryDtoValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Memory text is required.");

        RuleFor(x => x.AuthorDisplayName)
            .MaximumLength(300).WithMessage("Author display name must be at most 300 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.AuthorDisplayName));
    }
}