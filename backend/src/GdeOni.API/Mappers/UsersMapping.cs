using GdeOni.API.Models.Users;
using GdeOni.Application.Legal.Commands.AcceptLegal.Model;
using GdeOni.Application.Users.Commands.Block.Model;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Application.Users.Commands.ChangePassword.Model;
using GdeOni.Application.Users.Commands.ChangeRole.Model;
using GdeOni.Application.Users.Commands.Delete.Model;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Application.Users.Commands.Unblock.Model;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Application.Users.Queries.GetById.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// Request → Command/Query маппинг для контроллеров управления
/// пользователями и аутентификацией.
/// </summary>
public static class UsersMapping
{
    /// <summary>Маппит DTO регистрации в команду use case.</summary>
    public static RegisterUserCommand ToCommand(this RegisterUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new RegisterUserCommand(
            request.Email,
            request.UserName,
            request.FullName,
            request.Password,
            request.BirthDate,
            request.PrivacyPolicyAccepted,
            request.TermsAccepted);
    }

    /// <summary>Маппит DTO листинга пользователей в запрос use case.</summary>
    public static GetAllUsersQuery ToQuery(this GetAllUsersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetAllUsersQuery(
            request.Search,
            request.Role,
            request.RegisteredAtUtc,
            request.LastLoginAtUtc,
            request.Page,
            request.PageSize,
            request.RegisteredFromUtc,
            request.RegisteredToUtc);
    }

    /// <summary>Возвращает запрос пользователя по идентификатору.</summary>
    public static GetUserByIdQuery ToGetByIdQuery(Guid id) => new(id);

    /// <summary>Маппит DTO правки профиля в команду use case.</summary>
    public static UpdateUserProfileCommand ToCommand(this UpdateUserProfileRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateUserProfileCommand(
            id,
            request.UserName,
            request.FullName,
            request.CurrentPassword);
    }

    /// <summary>Маппит DTO смены пароля в команду use case.</summary>
    public static ChangePasswordCommand ToCommand(this ChangePasswordRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword);
    }

    /// <summary>Маппит DTO смены email в команду use case.</summary>
    public static ChangeEmailCommand ToCommand(this ChangeEmailRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ChangeEmailCommand(id, request.Email);
    }

    /// <summary>Маппит DTO смены роли в команду use case.</summary>
    public static ChangeRoleCommand ToCommand(this ChangeRoleRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ChangeRoleCommand(id, request.UserRole);
    }

    /// <summary>Возвращает команду удаления пользователя.</summary>
    public static DeleteUserCommand ToDeleteCommand(Guid id) => new(id);

    /// <summary>Маппит DTO блокировки в команду use case.</summary>
    public static BlockUserCommand ToBlockCommand(this BlockUserRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new BlockUserCommand(id, request.Reason);
    }

    /// <summary>Возвращает команду разблокировки пользователя.</summary>
    public static UnblockUserCommand ToUnblockCommand(Guid id) => new(id);

    /// <summary>Маппит DTO принятия Privacy/Terms в команду use case.</summary>
    public static AcceptLegalCommand ToCommand(this AcceptLegalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new AcceptLegalCommand(
            request.PrivacyPolicyVersion,
            request.TermsVersion);
    }
}
