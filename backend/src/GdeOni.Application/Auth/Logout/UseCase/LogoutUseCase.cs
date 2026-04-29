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
        var presentedHash = refreshTokenFactory.Hash(command.RefreshToken);

        var existingToken = await refreshTokenRepository.GetByHash(presentedHash, cancellationToken);
        if (existingToken is null || existingToken.IsRevoked)
            return UnitResult.Success<Error>();

        var revokeResult = existingToken.Revoke(DateTime.UtcNow);
        if (revokeResult.IsFailure)
            return revokeResult.Error;

        await refreshTokenRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
