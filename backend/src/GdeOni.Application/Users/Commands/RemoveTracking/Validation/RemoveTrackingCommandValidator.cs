using FluentValidation;
using GdeOni.Application.Users.Commands.RemoveTracking.Model;

namespace GdeOni.Application.Users.Commands.RemoveTracking.Validation;

public sealed class RemoveTrackingCommandValidator : AbstractValidator<RemoveTrackingCommand>
{
    public RemoveTrackingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.DeceasedId)
            .NotEmpty();
    }
}