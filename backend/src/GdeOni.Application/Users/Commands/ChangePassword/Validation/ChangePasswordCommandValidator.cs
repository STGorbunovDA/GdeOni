using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Constants;
using GdeOni.Application.Users.Commands.ChangePassword.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangePassword.Validation;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.User.IdRequired());

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithError(Errors.User.PasswordRequired())
            .MinimumLength(PasswordPolicy.MinPasswordLength)
            .WithError(Errors.User.PasswordTooShort(PasswordPolicy.MinPasswordLength))
            .MaximumLength(PasswordPolicy.MaxPasswordLength)
            .WithError(Errors.User.PasswordTooLong(PasswordPolicy.MaxPasswordLength));
    }
}
