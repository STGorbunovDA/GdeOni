using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Support.Commands.Create.Model;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.Create.Validation;

public sealed class CreateSupportTicketCommandValidator : AbstractValidator<CreateSupportTicketCommand>
{
    public CreateSupportTicketCommandValidator()
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
    }
}
