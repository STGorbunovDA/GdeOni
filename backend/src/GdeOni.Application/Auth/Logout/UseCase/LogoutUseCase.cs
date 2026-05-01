using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Logout.Model;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Logout.UseCase;

public sealed class LogoutUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenFactory refreshTokenFactory,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ILogoutUseCase
{
    public Task<UnitResult<Error>> Execute(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var presentedHash = refreshTokenFactory.Hash(command.RefreshToken);

        var existingToken = await refreshTokenRepository.GetByHash(presentedHash, cancellationToken);

        // Идемпотентный ответ для трёх случаев:
        // - токена не существует;
        // - токен уже отозван;
        // - токен принадлежит другому пользователю.
        // Семантически одинаковый Success скрывает от атакующего сам факт
        // существования чужого токена — нельзя ни перебирать, ни смотреть
        // по latency.
        if (existingToken is null
            || existingToken.IsRevoked
            || existingToken.UserId != currentUserIdResult.Value)
        {
            return UnitResult.Success<Error>();
        }

        var revokeResult = existingToken.Revoke(DateTime.UtcNow);
        if (revokeResult.IsFailure)
            return revokeResult.Error;

        await refreshTokenRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
