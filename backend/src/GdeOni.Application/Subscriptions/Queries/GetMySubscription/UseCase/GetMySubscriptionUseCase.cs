using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Queries.GetMySubscription.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetMySubscription.UseCase;

public sealed class GetMySubscriptionUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IGetMySubscriptionUseCase
{
    public async Task<Result<MySubscriptionResponse, Error>> Execute(CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var user = await userRepository.GetByIdReadOnly(currentUserIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var subscription = user.Subscription;
        var hasComplimentary = user.HasComplimentaryAccess(nowUtc);

        return Result.Success<MySubscriptionResponse, Error>(new MySubscriptionResponse(
            Status: subscription.Status.ToString(),
            Plan: subscription.Plan?.ToString(),
            ExpiresAtUtc: subscription.ExpiresAtUtc,
            CancelledAtUtc: subscription.CancelledAtUtc,
            // IsActiveNow без grace — grace применяется только на гейте.
            // Здесь это "видит ли пользователь подписку как активную".
            // D22: complimentary access тоже даёт IsActiveNow=true, чтобы
            // UI скрывал paywall и кнопку "Оформить подписку".
            IsActiveNow: subscription.IsActive(nowUtc) || hasComplimentary,
            IsOnTrial: subscription.IsOnTrial(nowUtc),
            DaysUntilExpiry: subscription.DaysUntilExpiry(nowUtc),
            HasComplimentaryAccess: hasComplimentary,
            ComplimentaryAccessUntilUtc: hasComplimentary ? user.ComplimentaryAccessUntilUtc : null,
            ComplimentaryAccessNote: hasComplimentary ? user.ComplimentaryAccessNote : null));
    }
}
