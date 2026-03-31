using FluentValidation;
using GdeOni.Application.Constants;
using GdeOni.Application.Users.ChangePassword.Model;

namespace GdeOni.Application.Users.ChangePassword.Validation;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(PasswordPolicy.MinPasswordLength);
    }
}