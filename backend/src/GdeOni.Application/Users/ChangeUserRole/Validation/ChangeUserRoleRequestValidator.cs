using FluentValidation;
using GdeOni.Application.Users.ChangeUserRole.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ChangeUserRole.Validation;

public sealed class ChangeUserRoleRequestValidator : AbstractValidator<ChangeUserRoleRequest>
{
    public ChangeUserRoleRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.UserRole)
            .IsInEnum()
            .WithMessage("Invalid user role")
            .NotEqual(UserRole.Unknown)
            .WithMessage("The role cannot be Unknown");
        
        RuleFor(x => x.UserRole)
            .NotEqual(UserRole.SuperAdmin)
            .WithMessage("The SuperAdmin role cannot be assigned");
    }
}