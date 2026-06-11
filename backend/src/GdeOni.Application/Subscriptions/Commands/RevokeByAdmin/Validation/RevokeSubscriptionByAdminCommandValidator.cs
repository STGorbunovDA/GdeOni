using FluentValidation;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.Model;

namespace GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.Validation;

public sealed class RevokeSubscriptionByAdminCommandValidator
    : AbstractValidator<RevokeSubscriptionByAdminCommand>
{
    public RevokeSubscriptionByAdminCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
