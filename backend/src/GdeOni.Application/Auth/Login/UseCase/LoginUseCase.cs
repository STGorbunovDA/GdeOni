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
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<LoginResponse, Error>> Handle(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmail(request.Email, cancellationToken);
        if (user is null)
            return Errors.User.InvalidCredentials();

        var isValid = passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isValid)
            return Errors.User.InvalidCredentials();

        var loginMarkResult = user.MarkLogin();
        if (loginMarkResult.IsFailure)
            return loginMarkResult.Error;

        await userRepository.Save(cancellationToken);

        var token = "Bearer " + jwtProvider.GenerateToken(user);

        return Result.Success<LoginResponse, Error>(new LoginResponse(
            user.Id,
            user.Email,
            user.UserName,
            user.FullName,
            user.Role.ToString(),
            token));
    }
}