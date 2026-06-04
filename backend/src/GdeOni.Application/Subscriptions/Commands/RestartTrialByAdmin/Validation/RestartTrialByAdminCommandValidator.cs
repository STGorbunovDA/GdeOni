using FluentValidation;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.Model;

namespace GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.Validation;

public sealed class RestartTrialByAdminCommandValidator : AbstractValidator<RestartTrialByAdminCommand>
{
    public RestartTrialByAdminCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DurationDays)
            .GreaterThan(0)
            .LessThanOrEqualTo(365)
            .When(x => x.DurationDays.HasValue);
    }
}
