using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Register.UseCase;

public sealed class RegisterUserUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRegisterUserUseCase
{
    public Task<Result<RegisterUserResponse, Error>> Execute(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(
            command,
            Handle,
            cancellationToken);
    }

    private async Task<Result<RegisterUserResponse, Error>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var finalUserName = string.IsNullOrWhiteSpace(command.UserName)
            ? normalizedEmail.Split('@')[0]
            : command.UserName.Trim();

        var emailExists = await userRepository.ExistsByEmail(
            normalizedEmail,
            cancellationToken);

        if (emailExists)
            return Errors.User.EmailAlreadyExists();

        var userNameExists = await userRepository.ExistsByUserName(
            finalUserName,
            cancellationToken);

        if (userNameExists)
            return Errors.User.UserNameAlreadyExists();

        var passwordHash = passwordHasher.Hash(command.Password);

        var userResult = User.Register(
            normalizedEmail,
            passwordHash,
            command.FullName,
            finalUserName);

        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.Value;

        await userRepository.Add(user, cancellationToken);
        await userRepository.Save(cancellationToken);
        return Result.Success<RegisterUserResponse, Error>(new RegisterUserResponse(user.Id));
        
    }
}