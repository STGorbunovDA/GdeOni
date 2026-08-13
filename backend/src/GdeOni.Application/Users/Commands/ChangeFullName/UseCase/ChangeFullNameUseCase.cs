using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.ChangeFullName.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeFullName.UseCase;

/// <summary>
/// Смена полного имени в профиле. Проверок уникальности нет — тёзки
/// допустимы; ограничение длины и trim делает домен.
///
/// В отличие от UpdateProfile (меняет ещё и UserName, ротируя SecurityStamp),
/// здесь сессии не закрываются: это отображаемое поле.
/// </summary>
public sealed class ChangeFullNameUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : IChangeFullNameUseCase
{
    public async Task<UnitResult<Error>> Execute(
        ChangeFullNameCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var user = await userRepository.GetById(currentUserIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        var result = user.ChangeFullName(command.FullName);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
