using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Login.UseCase;

public sealed class LoginUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ILoginUseCase
{
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

        var isValid = passwordHasher.Verify(command.Password, user.PasswordHash);
        if (!isValid)
            return Errors.User.InvalidCredentials();

        var loginMarkResult = user.MarkLogin();
        if (loginMarkResult.IsFailure)
            return loginMarkResult.Error;

        await userRepository.Save(cancellationToken);

        var token = jwtProvider.GenerateToken(user);

        return Result.Success<LoginResponse, Error>(new LoginResponse(
            user.Id,
            user.Email,
            user.UserName,
            user.FullName,
            user.Role.ToString(),
            token));
    }
}