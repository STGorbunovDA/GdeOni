using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.Validation;

public sealed class PromoteAttachmentToMainPhotoCommandValidator
    : AbstractValidator<PromoteAttachmentToMainPhotoCommand>
{
    public PromoteAttachmentToMainPhotoCommandValidator()
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
    }
}
