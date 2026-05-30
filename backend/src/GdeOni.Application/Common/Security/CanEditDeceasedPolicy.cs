using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Common.Security;

/// <summary>
/// D24. Реализация политики через <see cref="IUserRepository.IsActivelyTracking"/>.
/// Не лезет в БД для админа — они проверяются через <see cref="ICurrentUserService.IsAdmin"/>.
/// </summary>
public sealed class CanEditDeceasedPolicy(
    ICurrentUserService currentUserService,
    IUserRepository userRepository) : ICanEditDeceasedPolicy
{
    public async Task<UnitResult<Error>> CheckAsync(Guid deceasedId, CancellationToken cancellationToken)
    {
        if (currentUserService.IsAdmin())
            return UnitResult.Success<Error>();

        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var isTracking = await userRepository.IsActivelyTracking(
            currentUserIdResult.Value, deceasedId, cancellationToken);

        return isTracking
            ? UnitResult.Success<Error>()
            : Errors.DeceasedEdit.NotEditor();
    }
}
