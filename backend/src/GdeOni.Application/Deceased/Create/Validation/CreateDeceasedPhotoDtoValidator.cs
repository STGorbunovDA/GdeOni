using FluentValidation;
using GdeOni.Application.Deceased.Create.Model;

namespace GdeOni.Application.Deceased.Create.Validation;

public sealed class CreateDeceasedPhotoDtoValidator
    : AbstractValidator<CreateDeceasedPhotoDto>
{
    public CreateDeceasedPhotoDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Photo URL is required.")
            .MaximumLength(2000).WithMessage("Photo URL must be at most 2000 characters.")
            .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
            .WithMessage("Photo URL must be a valid absolute URL.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Photo description must be at most 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.AddedByUserId)
            .NotEmpty().WithMessage("AddedByUserId is required.");
    }
}