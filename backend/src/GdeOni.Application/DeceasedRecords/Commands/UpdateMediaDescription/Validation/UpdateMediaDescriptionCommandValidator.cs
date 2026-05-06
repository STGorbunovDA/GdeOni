using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Validation;

public sealed class UpdateMediaDescriptionCommandValidator
    : AbstractValidator<UpdateMediaDescriptionCommand>
{
    public UpdateMediaDescriptionCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.MediaId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("media_id"));

        // Description nullable — null/whitespace = очистить описание.
        // При непустом значении — лимит совпадает с DeceasedMedia.MaxDescriptionLength.
        RuleFor(x => x.Description!)
            .MaximumLength(DeceasedMedia.MaxDescriptionLength)
            .WithError(Errors.DeceasedMedia.DescriptionTooLong(DeceasedMedia.MaxDescriptionLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
