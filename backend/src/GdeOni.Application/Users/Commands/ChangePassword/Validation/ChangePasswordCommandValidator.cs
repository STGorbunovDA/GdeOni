using FluentValidation;
using GdeOni.Application.Constants;
using GdeOni.Application.Users.Commands.ChangePassword.Model;

namespace GdeOni.Application.Users.Commands.ChangePassword.Validation;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(PasswordPolicy.MinPasswordLength);
    }
}