using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.MarkRelativesSeen.UseCase;

/// <summary>
/// Фаза 4. Отмечает всех «новых» родственников текущего пользователя
/// просмотренными (сбрасывает is_new) — вызывается при заходе на вкладку
/// «Родственники», чтобы попап «События» и бейдж больше их не показывали.
/// </summary>
public sealed class MarkRelativesSeenUseCase(
    IRelativesRepository relativesRepository,
    ICurrentUserService currentUserService)
    : IMarkRelativesSeenUseCase
{
    public async Task<UnitResult<Error>> Execute(CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        await relativesRepository.MarkRelativesSeen(userIdResult.Value, cancellationToken);
        return UnitResult.Success<Error>();
    }
}
