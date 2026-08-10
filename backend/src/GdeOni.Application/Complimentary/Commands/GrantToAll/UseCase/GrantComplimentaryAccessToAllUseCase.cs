using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Complimentary.Commands.GrantToAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Complimentary.Commands.GrantToAll.UseCase;

/// <summary>
/// Массовая выдача бесплатного доступа ВСЕМ пользователям на N дней —
/// «мягкая подушка» перед возвратом платного режима, чтобы никто резко не
/// упёрся в paywall. В отличие от поштучной выдачи (Admin+SuperAdmin), это
/// только для SuperAdmin: операция уровня «включаю монетизацию».
///
/// Реализована bulk-UPDATE'ом (ExecuteUpdate в репозитории): только
/// продлевает доступ, у кого он короче, и не трогает тех, у кого уже выдан
/// на более поздний срок. Кеш доступа per-user не инвалидируем — у него TTL
/// ~30с, всё подтянется само; точечная инвалидация тысяч юзеров непрактична.
/// </summary>
public sealed class GrantComplimentaryAccessToAllUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IGrantComplimentaryAccessToAllUseCase
{
    private const int DefaultDurationDays = 30;
    private const int MaxDurationDays = 3650;
    private const string BulkNote = "Массовая выдача (переход на платный режим)";

    public async Task<Result<GrantComplimentaryAccessToAllResponse, Error>> Execute(
        GrantComplimentaryAccessToAllCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsSuperAdmin())
            return Errors.User.UserForbidden();

        var adminIdResult = currentUserService.GetCurrentUserId();
        if (adminIdResult.IsFailure)
            return adminIdResult.Error;

        var days = Math.Clamp(command.DurationDays ?? DefaultDurationDays, 1, MaxDurationDays);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var untilUtc = now.AddDays(days);

        var affected = await userRepository.GrantComplimentaryAccessToAll(
            untilUtc, adminIdResult.Value, BulkNote, now, cancellationToken);

        return Result.Success<GrantComplimentaryAccessToAllResponse, Error>(
            new GrantComplimentaryAccessToAllResponse(affected, untilUtc));
    }
}
