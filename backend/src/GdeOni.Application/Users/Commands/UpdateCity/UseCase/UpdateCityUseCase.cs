using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.UpdateCity.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateCity.UseCase;

/// <summary>
/// Пользователь указывает/меняет город в профиле. Домен делает no-op guard
/// (то же значение — без Touch) и НЕ ротирует SecurityStamp — город это
/// предпочтение, force-logout не нужен (зеркало SetRelativeConnectionsConsent).
/// </summary>
public sealed class UpdateCityUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : IUpdateCityUseCase
{
    public async Task<UnitResult<Error>> Execute(
        UpdateCityCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var user = await userRepository.GetById(currentUserIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        var result = user.UpdateCity(command.City);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
