using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Refresh.Model;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Aggregates.Auth;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Auth.Refresh.UseCase;

public sealed class RefreshUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtProvider jwtProvider,
    IRefreshTokenFactory refreshTokenFactory,
    ICurrentUserService currentUserService,
    IOptions<JwtOptions> jwtOptions,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRefreshUseCase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public Task<Result<RefreshResponse, Error>> Execute(
        RefreshCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<RefreshResponse, Error>> Handle(
        RefreshCommand command,
        CancellationToken cancellationToken)
    {
        var presentedHash = refreshTokenFactory.Hash(command.RefreshToken);

        var existingToken = await refreshTokenRepository.GetByHash(presentedHash, cancellationToken);
        if (existingToken is null)
            return Errors.RefreshToken.TokenInvalid();

        var nowUtc = DateTime.UtcNow;

        if (existingToken.IsRevoked)
            return Errors.RefreshToken.TokenRevoked();

        if (existingToken.IsExpired(nowUtc))
            return Errors.RefreshToken.TokenExpired();

        var user = await userRepository.GetById(existingToken.UserId, cancellationToken);
        if (user is null)
            return Errors.RefreshToken.TokenInvalid();

        var revokeResult = existingToken.Revoke(nowUtc);
        if (revokeResult.IsFailure)
            return revokeResult.Error;

        var accessToken = jwtProvider.GenerateAccessToken(user);

        var newPlain = refreshTokenFactory.Generate();
        var newHash = refreshTokenFactory.Hash(newPlain);
        var newExpiresAtUtc = nowUtc.AddDays(_jwtOptions.RefreshTokenLifetimeDays);

        var newTokenResult = RefreshToken.Issue(
            user.Id,
            newHash,
            newExpiresAtUtc,
            nowUtc,
            currentUserService.GetRemoteIpAddress());

        if (newTokenResult.IsFailure)
            return newTokenResult.Error;

        await refreshTokenRepository.Add(newTokenResult.Value, cancellationToken);
        await refreshTokenRepository.Save(cancellationToken);

        return Result.Success<RefreshResponse, Error>(new RefreshResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            newPlain,
            newExpiresAtUtc));
    }
}
