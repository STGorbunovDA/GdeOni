using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.Delete.Model;
using GdeOni.Domain.Aggregates.User;
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
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        if (command.UserId == currentUserIdResult.Value)
            return Errors.User.DeleteSelfForbidden();

        var user = await userRepository.GetById(command.UserId, cancellationToken);

        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

        // SuperAdmin неприкасаем. С учётом того, что SuperAdmin создаётся
        // только сидером (D7.14), это означает: после первой инициализации
        // его никто и никак не удалит — система всегда сохраняет хотя бы
        // одного держателя самой высокой роли.
        if (user.Role == UserRole.SuperAdmin)
            return Errors.User.DeleteSuperAdminForbidden();

        userRepository.Delete(user);
        // Refresh-токены удаляемого пользователя уйдут сами по
        // OnDelete Cascade (RefreshTokenConfiguration).
        await userRepository.Save(cancellationToken);

        return Result.Success<DeleteUserResponse, Error>(
            new DeleteUserResponse(command.UserId));
    }
}