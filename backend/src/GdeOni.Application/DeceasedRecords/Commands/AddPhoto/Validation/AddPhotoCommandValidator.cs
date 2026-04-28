using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Validation;

public sealed class AddPhotoCommandValidator : AbstractValidator<AddPhotoCommand>
{
    public AddPhotoCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

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
    }
}