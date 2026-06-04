using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.UseCase;

/// <summary>
/// Админский restart триала: переводит юзера в Trial с заданной
/// длительностью (default — из SubscriptionOptions). Защищён теми же
/// правилами что и RevokeSubscriptionByAdmin: не себе, Admin не
/// трогает SuperAdmin.
/// </summary>
public sealed class RestartTrialByAdminUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IOptions<SubscriptionOptions> subscriptionOptions,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRestartTrialByAdminUseCase
{
    public Task<UnitResult<Error>> Execute(
        RestartTrialByAdminCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        RestartTrialByAdminCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (currentUserIdResult.Value == command.UserId)
            return Errors.Subscription.RevokeSelfForbidden();

        var target = await userRepository.GetById(command.UserId, cancellationToken);
        if (target is null)
            return Errors.General.NotFound("user", command.UserId);

        if (target.Role == Domain.Shared.UserRole.SuperAdmin)
            return Errors.Subscription.ManageSuperAdminForbidden();

        var duration = command.DurationDays is > 0
            ? TimeSpan.FromDays(command.DurationDays.Value)
            : subscriptionOptions.Value.TrialDuration;

        var restartResult = target.RestartTrialByAdmin(DateTime.UtcNow, duration);
        if (restartResult.IsFailure)
            return restartResult.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
