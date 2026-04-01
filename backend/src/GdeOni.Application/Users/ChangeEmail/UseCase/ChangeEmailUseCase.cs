using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.ChangeEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ChangeEmail.UseCase;

public sealed class ChangeEmailUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangeEmailUseCase
{
    public Task<Result<ChangeEmailResponse, Error>> Execute(
        ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<ChangeEmailResponse, Error>> Handle(
        ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(request.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", request.UserId);

        var exists = await userRepository.ExistsByEmail(request.Email, cancellationToken);
        if (exists)
            return Errors.User.EmailAlreadyExists();
        
        var result = user.ChangeEmail(request.Email);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<ChangeEmailResponse, Error>(
            new ChangeEmailResponse(user.Id, user.Email));
    }
}