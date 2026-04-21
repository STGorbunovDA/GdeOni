using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.Validation;

public sealed class UpdatePhotoCommandValidator : AbstractValidator<UpdatePhotoCommand>
{
    public UpdatePhotoCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.PhotoId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("photo_id"));

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
    }
}