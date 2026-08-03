using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.SetRelativeConnectionsConsent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.SetRelativeConnectionsConsent.UseCase;

/// <summary>
/// Функция «Родственники»: пользователь сам включает/выключает согласие в
/// профиле. Домен делает no-op guard (то же значение — без Touch), а
/// SecurityStamp не ротируется — это предпочтение, force-logout не нужен.
/// </summary>
public sealed class SetRelativeConnectionsConsentUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : ISetRelativeConnectionsConsentUseCase
{
    public async Task<UnitResult<Error>> Execute(
        SetRelativeConnectionsConsentCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var user = await userRepository.GetById(currentUserIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        user.SetRelativeConnectionsConsent(command.Allow);
        await userRepository.Save(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
