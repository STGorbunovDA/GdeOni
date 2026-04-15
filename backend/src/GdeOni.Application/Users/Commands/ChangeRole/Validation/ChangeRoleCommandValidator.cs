using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.ChangeRole.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeRole.Validation;

public sealed class ChangeRoleCommandValidator : AbstractValidator<ChangeRoleCommand>
{
    public ChangeRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.User.IdRequired());

        RuleFor(x => x.UserRole)
            .IsInEnum()
            .WithError(Errors.User.RoleInvalid())
            .NotEqual(UserRole.Unknown)
            .WithError(Errors.User.RoleUnknownNotAllowed())
            .NotEqual(UserRole.SuperAdmin)
            .WithError(Errors.User.RoleSuperAdminNotAllowed());
    }
}