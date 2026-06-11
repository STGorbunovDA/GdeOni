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

        // CurrentPassword nullable: админ меняет чужой пароль без него.
        // Если значение есть — режем по верхнему лимиту до похода в
        // BCrypt.Verify (defense-in-depth, см. D7.59 / D7.54).
        RuleFor(x => x.CurrentPassword!)
            .MaximumLength(PasswordPolicy.MaxPasswordLength)
            .WithError(Errors.User.PasswordTooLong(PasswordPolicy.MaxPasswordLength))
            .When(x => !string.IsNullOrEmpty(x.CurrentPassword));

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithError(Errors.User.PasswordRequired())
            .MinimumLength(PasswordPolicy.MinPasswordLength)
            .WithError(Errors.User.PasswordTooShort(PasswordPolicy.MinPasswordLength))
            .MaximumLength(PasswordPolicy.MaxPasswordLength)
            .WithError(Errors.User.PasswordTooLong(PasswordPolicy.MaxPasswordLength));
    }
}
