using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeEmail.UseCase;

public sealed class ChangeEmailUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangeEmailUseCase
{
    public Task<Result<ChangeEmailResponse, Error>> Execute(
        ChangeEmailCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ChangeEmailResponse, Error>> Handle(
        ChangeEmailCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        var email = command.Email.Trim();

        var exists = await userRepository.ExistsByEmail(email, cancellationToken);
        if (exists && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            return Errors.User.EmailAlreadyExists();

        var result = user.ChangeEmail(email);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<ChangeEmailResponse, Error>(
            new ChangeEmailResponse(user.Id, user.Email));
    }
}