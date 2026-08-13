using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.ChangeLogin.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeLogin.UseCase;

/// <summary>
/// Смена собственного логина. Двух одинаковых логинов быть не может:
/// проверяем занятость ДО записи (исключая себя — иначе сохранение своего же
/// логина отбивалось бы как «занят»), а гонку двух параллельных запросов
/// добивает уникальный индекс ux_users_login → UniqueConstraintException.
///
/// SecurityStamp не ротируется (см. User.ChangeLogin): логина нет в токене,
/// выкидывать человека из сессий незачем.
/// </summary>
public sealed class ChangeLoginUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IChangeLoginUseCase
{
    public Task<UnitResult<Error>> Execute(
        ChangeLoginCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        ChangeLoginCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var userId = currentUserIdResult.Value;

        // Нормализуем той же доменной функцией, что и запись: проверять
        // занятость надо ровно того значения, которое ляжет в БД.
        var normalizedResult = User.NormalizeLogin(command.Login);
        if (normalizedResult.IsFailure)
            return normalizedResult.Error;

        var taken = await userRepository.ExistsByLoginExceptUser(
            normalizedResult.Value, userId, cancellationToken);
        if (taken)
            return Errors.User.LoginAlreadyExists();

        var user = await userRepository.GetById(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var changeResult = user.ChangeLogin(normalizedResult.Value);
        if (changeResult.IsFailure)
            return changeResult.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
