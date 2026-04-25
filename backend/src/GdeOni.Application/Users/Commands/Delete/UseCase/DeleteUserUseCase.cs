using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.Delete.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Delete.UseCase;

public sealed class DeleteUserUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IDeleteUserUseCase
{
    public Task<Result<DeleteUserResponse, Error>> Execute(
        DeleteUserCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<DeleteUserResponse, Error>> Handle(
        DeleteUserCommand command,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), 
            UserRole.Admin.ToString());
        
        if (!isAdmin)
            return Errors.User.UserForbidden();
        
        var user = await userRepository.GetById(command.UserId, cancellationToken);

        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        userRepository.Delete(user);
        await userRepository.Save(cancellationToken);

        return Result.Success<DeleteUserResponse, Error>(
            new DeleteUserResponse(command.UserId));
    }
}