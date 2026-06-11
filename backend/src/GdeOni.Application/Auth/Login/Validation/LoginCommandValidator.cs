using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Application.Constants;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Login.Validation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithError(Errors.User.EmailRequired())
            .EmailAddress()
            .WithError(Errors.User.EmailInvalid())
            .MaximumLength(User.MaxEmailLength)
            .WithError(Errors.User.EmailTooLong(User.MaxEmailLength));

        // MinimumLength НЕ ставим: пользователи, зарегистрированные до
        // D7.54, имеют пароли короче MinPasswordLength — иначе они не
        // смогут войти. MaximumLength закрывает DoS через гигантский
        // body, дальше BCrypt-усечение ловит D7.54. См. D7.66.
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(Errors.User.PasswordRequired())
            .MaximumLength(PasswordPolicy.MaxPasswordLength)
            .WithError(Errors.User.PasswordTooLong(PasswordPolicy.MaxPasswordLength));
    }
}