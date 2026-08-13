using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.AssignMissingLogins.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.AssignMissingLogins.UseCase;

/// <summary>
/// Массово проставить логин тем, у кого его нет: логин = часть email до «@»,
/// а если она уже занята — ПОЛНЫЙ email (правило то же, что при регистрации и
/// в миграции AddUserLogin).
///
/// Только SuperAdmin: операция затрагивает чужие учётные записи целиком.
/// Идемпотентна — второй запуск не найдёт пустых логинов и вернёт 0.
/// Пользователей обрабатываем по одному, проверяя занятость по факту: внутри
/// одного прохода два «bous07» не должны получить одинаковый логин.
/// </summary>
public sealed class AssignMissingLoginsUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : IAssignMissingLoginsUseCase
{
    public async Task<Result<AssignMissingLoginsResponse, Error>> Execute(
        AssignMissingLoginsCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsSuperAdmin())
            return Errors.User.UserForbidden();

        var pending = await userRepository.GetUsersWithoutLogin(cancellationToken);
        var assigned = 0;

        foreach (var (userId, email) in pending)
        {
            var prefix = User.GenerateLoginFromEmail(email);

            var login = await userRepository.ExistsByLogin(prefix, cancellationToken)
                ? User.LoginFromFullEmail(email)
                : prefix;

            // Полный email тоже занят — пропускаем, чтобы не словить нарушение
            // уникального индекса. Штатно недостижимо: email уникален.
            if (await userRepository.ExistsByLogin(login, cancellationToken))
                continue;

            assigned += await userRepository.SetLoginById(userId, login, cancellationToken);
        }

        return Result.Success<AssignMissingLoginsResponse, Error>(
            new AssignMissingLoginsResponse(assigned));
    }
}
