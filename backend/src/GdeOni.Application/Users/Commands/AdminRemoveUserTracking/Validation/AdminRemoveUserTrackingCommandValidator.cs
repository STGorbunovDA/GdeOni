using FluentValidation;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.Model;

namespace GdeOni.Application.Users.Commands.AdminRemoveUserTracking.Validation;

public sealed class AdminRemoveUserTrackingCommandValidator
    : AbstractValidator<AdminRemoveUserTrackingCommand>
{
    public AdminRemoveUserTrackingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DeceasedId).NotEmpty();
    }
}

public sealed class AdminRemoveAllUserTrackingCommandValidator
    : AbstractValidator<AdminRemoveAllUserTrackingCommand>
{
    public AdminRemoveAllUserTrackingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
