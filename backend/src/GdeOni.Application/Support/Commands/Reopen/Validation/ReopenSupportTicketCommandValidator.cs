using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.Reopen.Model;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.Reopen.Validation;

public sealed class ReopenSupportTicketCommandValidator
    : AbstractValidator<ReopenSupportTicketCommand>
{
    public ReopenSupportTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("ticketId"));

        RuleFor(x => x.UserReply)
            .MaximumLength(SupportTicket.MaxUserReplyLength)
            .WithError(Errors.Support.UserReplyTooLong(SupportTicket.MaxUserReplyLength))
            .When(x => !string.IsNullOrWhiteSpace(x.UserReply));
    }
}
