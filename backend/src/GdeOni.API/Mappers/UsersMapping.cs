using GdeOni.API.Models.Users;
using GdeOni.Application.Legal.Commands.AcceptLegal.Model;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Application.Users.Commands.ChangePassword.Model;
using GdeOni.Application.Users.Commands.ChangeRole.Model;
using GdeOni.Application.Users.Commands.Delete.Model;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Application.Users.Queries.GetById.Model;

namespace GdeOni.API.Mappers;

public static class UsersMapping
{
    public static RegisterUserCommand ToCommand(this RegisterUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new RegisterUserCommand(
            request.Email,
            request.UserName,
            request.FullName,
            request.Password,
            request.PrivacyPolicyAccepted,
            request.TermsAccepted);
    }

    public static GetAllUsersQuery ToQuery(this GetAllUsersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetAllUsersQuery(
            request.Search,
            request.Role,
            request.RegisteredAtUtc,
            request.LastLoginAtUtc,
            request.Page,
            request.PageSize);
    }

    public static GetUserByIdQuery ToGetByIdQuery(Guid id) => new(id);

    public static UpdateUserProfileCommand ToCommand(this UpdateUserProfileRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateUserProfileCommand(id, request.UserName, request.FullName);
    }

    public static ChangePasswordCommand ToCommand(this ChangePasswordRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword);
    }

    public static ChangeEmailCommand ToCommand(this ChangeEmailRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ChangeEmailCommand(id, request.Email);
    }

    public static ChangeRoleCommand ToCommand(this ChangeRoleRequest request, Guid id)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ChangeRoleCommand(id, request.UserRole);
    }

    public static DeleteUserCommand ToDeleteCommand(Guid id) => new(id);

    public static AcceptLegalCommand ToCommand(this AcceptLegalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new AcceptLegalCommand(
            request.PrivacyPolicyVersion,
            request.TermsVersion);
    }
}
