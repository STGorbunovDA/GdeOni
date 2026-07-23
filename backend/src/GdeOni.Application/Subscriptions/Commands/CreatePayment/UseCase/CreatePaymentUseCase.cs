using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;
using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Subscriptions.Commands.CreatePayment.UseCase;

public sealed class CreatePaymentUseCase(
    IUserRepository userRepository,
    ISubscriptionPaymentRepository paymentRepository,
    ICurrentUserService currentUserService,
    IPaymentProvider paymentProvider,
    IOptions<SubscriptionOptions> subscriptionOptions,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : ICreatePaymentUseCase
{
    public Task<Result<CreatePaymentResponse, Error>> Execute(
        CreatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<CreatePaymentResponse, Error>> Handle(
        CreatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;
        var userId = currentUserIdResult.Value;

        var user = await userRepository.GetById(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var options = subscriptionOptions.Value;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // D23. Re-use существующего Pending в течение
        // PendingPaymentReuseTimeout: если юзер тапнул "Оформить" два
        // раза подряд, второй вызов вернёт тот же CheckoutUrl вместо
        // создания нового платежа в YooKassa. Это закрывает кейс
        // multi-tap → "оплатил не последний платёж → backend его не
        // находит".
        var existingPending = await paymentRepository.GetActivePendingForUser(
            userId, options.PendingPaymentReuseTimeout, nowUtc, cancellationToken);

        if (existingPending is not null)
        {
            return Result.Success<CreatePaymentResponse, Error>(
                new CreatePaymentResponse(existingPending.CheckoutUrl!, existingPending.ExternalPaymentId));
        }

        // Создаём платёж у провайдера ДО мутации User: если YooKassa
        // отдала ошибку — User остаётся с прежним Subscription,
        // никакого "висящего" PendingPayment. Save() ниже атомарно
        // фиксирует "у нас есть paymentId от провайдера".
        //
        // ReturnUrl выбираем по клиенту: mobile — deep-link, web —
        // страница /payment/return нашего SPA (см. SubscriptionOptions).
        var paymentResult = await paymentProvider.CreateAsync(
            userId,
            options.MonthlyPriceRub,
            options.ProductDescription,
            options.GetReturnUrl(command.Platform),
            cancellationToken);

        if (paymentResult.IsFailure)
            return paymentResult.Error;

        var payment = paymentResult.Value;

        // D23. Запись истории платежа. Создаётся в Pending; webhook
        // переведёт в Succeeded/Cancelled/Failed.
        var recordResult = SubscriptionPayment.Create(
            userId,
            payment.ExternalPaymentId,
            command.Plan,
            options.MonthlyPriceRub,
            payment.CheckoutUrl,
            nowUtc);

        if (recordResult.IsFailure)
            return recordResult.Error;

        await paymentRepository.Add(recordResult.Value, cancellationToken);

        // D16 + D23. Оставляем LastPaymentId на User для backward-compat
        // (UI читает /me/subscription), хотя источник истины для
        // webhook-lookup теперь — subscription_payments.
        var requestResult = user.RequestSubscriptionPayment(command.Plan, payment.ExternalPaymentId);
        if (requestResult.IsFailure)
            return requestResult.Error;

        // Один DbContext — Save через любой репозиторий зафиксирует
        // и payment, и юзера в одной транзакции.
        await userRepository.Save(cancellationToken);

        return Result.Success<CreatePaymentResponse, Error>(
            new CreatePaymentResponse(payment.CheckoutUrl, payment.ExternalPaymentId));
    }
}
