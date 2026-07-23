using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.ForceClose.Model;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.ForceClose.Validation;

public sealed class ForceCloseSupportTicketCommandValidator
    : AbstractValidator<ForceCloseSupportTicketCommand>
{
    public ForceCloseSupportTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("ticketId"));

        // Обязательность и обрезка пробелов — в домене (ForceClose).
        // Здесь только отсекаем заведомо длинную строку до Save.
        RuleFor(x => x.CloseNote)
            .NotEmpty()
            .WithError(Errors.Support.ResolutionNoteRequired());

        RuleFor(x => x.CloseNote)
            .MaximumLength(SupportTicket.MaxResolutionNoteLength)
            .WithError(Errors.Support.ResolutionNoteTooLong(SupportTicket.MaxResolutionNoteLength))
            .When(x => !string.IsNullOrWhiteSpace(x.CloseNote));
    }
}
