using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.UpdateStatus.Model;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.UpdateStatus.Validation;

public sealed class UpdateSupportTicketStatusCommandValidator
    : AbstractValidator<UpdateSupportTicketStatusCommand>
{
    public UpdateSupportTicketStatusCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("ticketId"));

        RuleFor(x => x.Status)
            .Must(s => Enum.IsDefined(typeof(SupportTicketStatus), s) && s != SupportTicketStatus.Unknown)
            .WithError(Errors.Support.StatusInvalid());

        // Длина resolution_note ловится в Domain (там же проверяется
        // обязательность для Resolved). Здесь только обрезаем явно
        // длинные строки чтобы не гонять их в Save.
        RuleFor(x => x.ResolutionNote)
            .MaximumLength(SupportTicket.MaxResolutionNoteLength)
            .WithError(Errors.Support.ResolutionNoteTooLong(SupportTicket.MaxResolutionNoteLength))
            .When(x => !string.IsNullOrWhiteSpace(x.ResolutionNote));
    }
}
