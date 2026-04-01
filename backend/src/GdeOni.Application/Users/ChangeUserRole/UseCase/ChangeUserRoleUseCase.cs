using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.ChangeUserRole.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ChangeUserRole.UseCase;

public sealed class ChangeUserRoleUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangeUserRoleUseCase
{
    public Task<Result<ChangeUserRoleResponse, Error>> Execute(
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<ChangeUserRoleResponse, Error>> Handle(
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(request.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", request.UserId);

        var result = user.ChangeRole(request.UserRole);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<ChangeUserRoleResponse, Error>(
            new ChangeUserRoleResponse(user.Id));
    }
}