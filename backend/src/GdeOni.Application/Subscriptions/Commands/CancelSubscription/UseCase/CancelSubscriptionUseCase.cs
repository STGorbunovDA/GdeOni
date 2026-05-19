using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Commands.CancelSubscription.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.CancelSubscription.UseCase;

public sealed class CancelSubscriptionUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : ICancelSubscriptionUseCase
{
    public async Task<UnitResult<Error>> Execute(
        CancelSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var user = await userRepository.GetById(currentUserIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        var cancelResult = user.CancelSubscription(DateTime.UtcNow);
        if (cancelResult.IsFailure)
            return cancelResult.Error;

        await userRepository.Save(cancellationToken);
        // Внешний CancelRecurringAsync у YooKassa не нужен — мы работаем
        // в single-payment-модели (каждое продление = новый платёж).
        // Если когда-нибудь подключим saved-card auto-charge — добавим
        // здесь paymentProvider.CancelRecurringAsync(LastPaymentId).
        return UnitResult.Success<Error>();
    }
}
