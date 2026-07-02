using FluentValidation;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.CreatePayment.Validation;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.Plan)
            .Must(plan => System.Enum.IsDefined(typeof(SubscriptionPlan), plan))
            .WithErrorCode("subscription.plan.invalid")
            .WithMessage("Subscription plan is invalid.");

        RuleFor(x => x.Platform)
            .Must(p => System.Enum.IsDefined(typeof(ClientPlatform), p))
            .WithErrorCode("subscription.platform.invalid")
            .WithMessage("Client platform is invalid.");
    }
}
