using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;

public sealed class ProcessPaymentWebhookUseCase(
    IUserRepository userRepository,
    IPaymentProvider paymentProvider,
    IOptions<SubscriptionOptions> subscriptionOptions)
    : IProcessPaymentWebhookUseCase
{
    public async Task<UnitResult<Error>> Execute(
        ProcessPaymentWebhookCommand command,
        CancellationToken cancellationToken)
    {
        // Шаг 1: верификация подписи. Если фейл — Failure уходит в
        // контроллер, 401 без обращения к БД. Защита от replay-атак.
        var verificationResult = await paymentProvider.VerifyWebhookAsync(
            command.Payload,
            command.SignatureHeader,
            cancellationToken);

        if (verificationResult.IsFailure)
            return verificationResult.Error;

        var verification = verificationResult.Value;

        // Шаг 2: поиск юзера по externalPaymentId. Pending / Cancelled
        // тоже могут прилететь — но мы ничего не делаем кроме лога.
        // Если юзер не найден — 404; webhook от неизвестного платежа
        // считаем подделкой (или старый, уже почищенный платёж).
        var user = await userRepository.GetBySubscriptionPaymentId(
            verification.ExternalPaymentId,
            cancellationToken);
        if (user is null)
            return Errors.Subscription.PaymentNotFound();

        // Шаг 3: реакция на статус.
        switch (verification.Status)
        {
            case PaymentStatus.Succeeded:
                var nowUtc = DateTime.UtcNow;
                var expiresAt = nowUtc + subscriptionOptions.Value.MonthlyDuration;
                var plan = user.Subscription.Plan ?? Domain.Shared.SubscriptionPlan.Monthly;

                var activateResult = user.ActivateSubscription(
                    plan, nowUtc, expiresAt, verification.ExternalPaymentId);
                if (activateResult.IsFailure)
                    return activateResult.Error;
                break;

            case PaymentStatus.Cancelled:
                // Платёж отменился на стороне YooKassa — Subscription
                // остаётся в PendingPayment / Trial. UI при следующем
                // GET /me/subscription увидит Status и решит, что
                // делать. Никакой автокатки до прежнего статуса —
                // юзер всегда может сделать новый CreatePayment.
                break;

            case PaymentStatus.Pending:
                // Промежуточный статус — игнорируем. YooKassa пришлёт
                // ещё один webhook позже с финальным.
                return UnitResult.Success<Error>();
        }

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
