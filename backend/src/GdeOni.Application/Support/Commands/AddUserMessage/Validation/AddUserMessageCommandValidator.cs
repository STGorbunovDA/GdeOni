using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.AddUserMessage.Model;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.AddUserMessage.Validation;

public sealed class AddUserMessageCommandValidator
    : AbstractValidator<AddUserMessageCommand>
{
    public AddUserMessageCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("ticketId"));

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithError(Errors.Support.MessageTextRequired())
            .MaximumLength(SupportTicketMessage.MaxTextLength)
            .WithError(Errors.Support.MessageTextTooLong(SupportTicketMessage.MaxTextLength));
    }
}
