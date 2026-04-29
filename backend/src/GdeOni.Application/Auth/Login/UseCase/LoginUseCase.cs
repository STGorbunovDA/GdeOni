using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Aggregates.Auth;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Auth.Login.UseCase;

public sealed class LoginUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    IRefreshTokenFactory refreshTokenFactory,
    ICurrentUserService currentUserService,
    IOptions<JwtOptions> jwtOptions,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ILoginUseCase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public Task<Result<LoginResponse, Error>> Execute(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<LoginResponse, Error>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmail(command.Email, cancellationToken);
        if (user is null)
            return Errors.User.InvalidCredentials();

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            return Errors.User.InvalidCredentials();

        var loginMarkResult = user.MarkLogin();
        if (loginMarkResult.IsFailure)
            return loginMarkResult.Error;

        var accessToken = jwtProvider.GenerateAccessToken(user);

        var nowUtc = DateTime.UtcNow;
        var refreshTokenPlain = refreshTokenFactory.Generate();
        var refreshTokenHash = refreshTokenFactory.Hash(refreshTokenPlain);
        var refreshExpiresAtUtc = nowUtc.AddDays(_jwtOptions.RefreshTokenLifetimeDays);

        var refreshTokenResult = RefreshToken.Issue(
            user.Id,
            refreshTokenHash,
            refreshExpiresAtUtc,
            nowUtc,
            currentUserService.GetRemoteIpAddress());

        if (refreshTokenResult.IsFailure)
            return refreshTokenResult.Error;

        await refreshTokenRepository.Add(refreshTokenResult.Value, cancellationToken);
        await refreshTokenRepository.Save(cancellationToken);

        return Result.Success<LoginResponse, Error>(new LoginResponse(
            user.Id,
            user.Email,
            user.UserName,
            user.FullName,
            user.Role.ToString(),
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshTokenPlain,
            refreshExpiresAtUtc));
    }
}
