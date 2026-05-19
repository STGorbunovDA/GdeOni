using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Queries.GetMySubscription.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetMySubscription.UseCase;

public sealed class GetMySubscriptionUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
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

        var nowUtc = DateTime.UtcNow;
        var subscription = user.Subscription;

        return Result.Success<MySubscriptionResponse, Error>(new MySubscriptionResponse(
            Status: subscription.Status.ToString(),
            Plan: subscription.Plan?.ToString(),
            ExpiresAtUtc: subscription.ExpiresAtUtc,
            CancelledAtUtc: subscription.CancelledAtUtc,
            // IsActiveNow без grace — grace применяется только на гейте.
            // Здесь это "видит ли пользователь подписку как активную".
            IsActiveNow: subscription.IsActive(nowUtc),
            IsOnTrial: subscription.IsOnTrial(nowUtc),
            DaysUntilExpiry: subscription.DaysUntilExpiry(nowUtc)));
    }
}
