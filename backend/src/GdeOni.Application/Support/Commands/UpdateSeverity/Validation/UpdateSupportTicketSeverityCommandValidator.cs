using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.UpdateSeverity.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.UpdateSeverity.Validation;

public sealed class UpdateSupportTicketSeverityCommandValidator
    : AbstractValidator<UpdateSupportTicketSeverityCommand>
{
    public UpdateSupportTicketSeverityCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("ticketId"));

        RuleFor(x => x.Severity)
            .Must(s => Enum.IsDefined(typeof(SupportTicketSeverity), s) && s != SupportTicketSeverity.Unknown)
            .WithError(Errors.Support.SeverityInvalid());
    }
}
