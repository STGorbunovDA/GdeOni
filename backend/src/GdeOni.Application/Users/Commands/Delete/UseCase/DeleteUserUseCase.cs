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
    ISecurityStampInvalidator securityStampInvalidator,
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

        // Admin не может удалить другого Admin — симметрично с
        // ChangeRoleUseCase (D7.18). Защищает от admin-vs-admin войн
        // и тихого "выпиливания" коллег. Снять Admin может только
        // SuperAdmin. См. D7.70.
        var isSuperAdmin = currentUserService.IsInRole(nameof(UserRole.SuperAdmin));
        if (user.Role == UserRole.Admin && !isSuperAdmin)
            return Errors.User.DeletePeerAdminForbidden();

        userRepository.Delete(user);
        // Refresh-токены удаляемого пользователя уйдут сами по
        // OnDelete Cascade (RefreshTokenConfiguration).
        await userRepository.Save(cancellationToken);
        // D11.10.1: после удаления user'а access-токены у атакующего
        // живут до истечения SecurityStampCacheTtlSeconds, потому что
        // OnTokenValidated проверяет stamp по кешу. Cache.Remove
        // закрывает окно: следующий запрос пойдёт в БД, увидит, что
        // юзера нет, и завалит токен.
        securityStampInvalidator.Invalidate(user.Id);

        return Result.Success<DeleteUserResponse, Error>(
            new DeleteUserResponse(command.UserId));
    }
}