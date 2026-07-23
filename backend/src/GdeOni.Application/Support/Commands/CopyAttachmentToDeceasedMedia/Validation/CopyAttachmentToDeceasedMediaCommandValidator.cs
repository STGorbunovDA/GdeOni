using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.Validation;

public sealed class CopyAttachmentToDeceasedMediaCommandValidator
    : AbstractValidator<CopyAttachmentToDeceasedMediaCommand>
{
    public CopyAttachmentToDeceasedMediaCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEqual(Guid.Empty)
            .WithError(Errors.General.ValueIsRequired("ticketId"));

        RuleFor(x => x.AttachmentId)
            .NotEqual(Guid.Empty)
            .WithError(Errors.General.ValueIsRequired("attachmentId"));

        RuleFor(x => x.DeceasedId)
            .NotEqual(Guid.Empty)
            .WithError(Errors.General.ValueIsRequired("deceasedId"));

        RuleFor(x => x.MediaKind)
            .Must(k => k is MediaKind.DeceasedPhoto or MediaKind.GravePhoto or MediaKind.Document)
            .WithError(Errors.DeceasedMedia.KindInvalid());

        // MakeMain имеет смысл только для DeceasedPhoto.
        RuleFor(x => x)
            .Must(x => !x.MakeMain || x.MediaKind == MediaKind.DeceasedPhoto)
            .WithError(Errors.DeceasedMedia.OnlyDeceasedPhotoCanBeMain());
    }
}
