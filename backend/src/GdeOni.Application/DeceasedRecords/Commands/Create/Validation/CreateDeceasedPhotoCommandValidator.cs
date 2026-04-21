using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Create.Validation;

public sealed class CreateDeceasedPhotoCommandValidator
    : AbstractValidator<CreateDeceasedPhotoCommand>
{
    public CreateDeceasedPhotoCommandValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithError(Errors.DeceasedPhoto.UrlRequired())
            .MaximumLength(2000)
            .WithError(Errors.DeceasedPhoto.UrlTooLong(2000))
            .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
            .WithError(Errors.DeceasedPhoto.UrlInvalid());

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithError(Errors.DeceasedPhoto.DescriptionTooLong(1000))
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.AddedByUserId)
            .NotEmpty()
            .WithError(Errors.DeceasedPhoto.AddedByRequired());
    }
}