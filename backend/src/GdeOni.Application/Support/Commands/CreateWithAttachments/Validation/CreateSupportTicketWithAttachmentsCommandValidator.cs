using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.CreateWithAttachments.Model;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.CreateWithAttachments.Validation;

public sealed class CreateSupportTicketWithAttachmentsCommandValidator
    : AbstractValidator<CreateSupportTicketWithAttachmentsCommand>
{
    public CreateSupportTicketWithAttachmentsCommandValidator()
    {
        RuleFor(x => x.Kind)
            .Must(k => Enum.IsDefined(typeof(SupportTicketKind), k) && k != SupportTicketKind.Unknown)
            .WithError(Errors.Support.KindInvalid());

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithError(Errors.Support.TitleRequired())
            .MaximumLength(SupportTicket.MaxTitleLength)
            .WithError(Errors.Support.TitleTooLong(SupportTicket.MaxTitleLength));

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithError(Errors.Support.DescriptionRequired())
            .MaximumLength(SupportTicket.MaxDescriptionLength)
            .WithError(Errors.Support.DescriptionTooLong(SupportTicket.MaxDescriptionLength));

        // Количество вложений — упреждающая проверка ещё до того, как
        // мы тронули MinIO. Domain тоже проверит при AddAttachment,
        // но здесь — fail-fast'ом, не делая 6 загрузок впустую.
        RuleFor(x => x.Attachments)
            .NotNull()
            .Must(a => a.Count >= 1)
            .WithError(Error.Validation(
                "support_ticket.attachments.empty",
                "Use the non-multipart endpoint when there are no attachments."))
            .Must(a => a.Count <= SupportTicket.MaxAttachmentsPerTicket)
            .WithError(Errors.Support.AttachmentsLimitExceeded(SupportTicket.MaxAttachmentsPerTicket));
    }
}
