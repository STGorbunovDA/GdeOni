using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Validation;

public sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.Kind)
            .IsInEnum()
            .WithError(Errors.DeceasedMedia.KindInvalid());

        RuleFor(x => x.OriginalFileName)
            .NotEmpty()
            .WithError(Errors.DeceasedMedia.OriginalFileNameRequired())
            .MaximumLength(DeceasedMedia.MaxOriginalFileNameLength)
            .WithError(Errors.DeceasedMedia.OriginalFileNameTooLong(DeceasedMedia.MaxOriginalFileNameLength));

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithError(Errors.DeceasedMedia.ContentTypeRequired())
            .MaximumLength(DeceasedMedia.MaxContentTypeLength)
            .WithError(Errors.DeceasedMedia.ContentTypeTooLong(DeceasedMedia.MaxContentTypeLength));

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .WithError(Errors.DeceasedMedia.SizeBytesInvalid());

        RuleFor(x => x.Content)
            .NotNull()
            .WithError(Errors.Media.FileRequired());

        RuleFor(x => x.Description)
            .MaximumLength(DeceasedMedia.MaxDescriptionLength)
            .WithError(Errors.DeceasedMedia.DescriptionTooLong(DeceasedMedia.MaxDescriptionLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
