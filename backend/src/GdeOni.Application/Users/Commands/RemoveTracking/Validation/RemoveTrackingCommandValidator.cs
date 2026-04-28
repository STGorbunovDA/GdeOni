using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.RemoveTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.RemoveTracking.Validation;

public sealed class RemoveTrackingCommandValidator : AbstractValidator<RemoveTrackingCommand>
{
    public RemoveTrackingCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Tracking.DeceasedIdRequired());
    }
}