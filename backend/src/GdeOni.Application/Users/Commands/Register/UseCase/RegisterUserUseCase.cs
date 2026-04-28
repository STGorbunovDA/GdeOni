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
        var emailExists = await userRepository.ExistsByEmail(command.Email, cancellationToken);
        if (emailExists)
            return Errors.User.EmailAlreadyExists();

        // Вычисляем эффективный username для проверки уникальности
        // (домен применяет ту же логику при создании пользователя)
        var effectiveUserName = string.IsNullOrWhiteSpace(command.UserName)
            ? command.Email.Trim().ToLowerInvariant().Split('@')[0]
            : command.UserName.Trim().ToLowerInvariant();

        var userNameExists = await userRepository.ExistsByUserName(effectiveUserName, cancellationToken);
        if (userNameExists)
            return Errors.User.UserNameAlreadyExists();

        var passwordHash = passwordHasher.Hash(command.Password);

        var userResult = User.Register(
            command.Email,
            passwordHash,
            command.FullName,
            command.UserName);

        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.Value;

        await userRepository.Add(user, cancellationToken);
        await userRepository.Save(cancellationToken);
        return Result.Success<RegisterUserResponse, Error>(new RegisterUserResponse(user.Id));
        
    }
}