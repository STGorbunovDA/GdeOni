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
        // Вход принимает email ИЛИ логин, поэтому EmailAddress()-правила
        // здесь больше нет: оно отбивало вход по логину («невалидный email»)
        // ещё до обращения к БД. Существование учётки проверяет use case и
        // отвечает единым InvalidCredentials — по ошибке валидации нельзя
        // отличить «нет такого логина» от «неверный пароль».
        RuleFor(x => x.EmailOrLogin)
            .NotEmpty()
            .WithError(Errors.User.EmailRequired())
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