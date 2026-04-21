using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
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
            .MaximumLength(DeceasedPhoto.MaxUrlLength)
            .WithError(Errors.DeceasedPhoto.UrlTooLong(DeceasedPhoto.MaxUrlLength))
            .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
            .WithError(Errors.DeceasedPhoto.UrlInvalid());

        RuleFor(x => x.Description)
            .MaximumLength(DeceasedPhoto.MaxDescriptionLength)
            .WithError(Errors.DeceasedPhoto.DescriptionTooLong(DeceasedPhoto.MaxDescriptionLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.AddedByUserId)
            .NotEmpty()
            .WithError(Errors.DeceasedPhoto.AddedByRequired());
    }
}