using FluentValidation;
using GdeOni.Application.DeceasedRecords.UpdatePhoto.Model;

namespace GdeOni.Application.DeceasedRecords.UpdatePhoto.Validation;

public sealed class UpdatePhotoRequestValidator : AbstractValidator<UpdatePhotoRequest>
{
    public UpdatePhotoRequestValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty();

        RuleFor(x => x.PhotoId)
            .NotEmpty();

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
            .WithMessage("Photo URL must be a valid absolute URL.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}