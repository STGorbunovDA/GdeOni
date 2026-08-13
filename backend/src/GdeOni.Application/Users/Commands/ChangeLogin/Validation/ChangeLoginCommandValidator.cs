using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.ChangeLogin.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeLogin.Validation;

/// <summary>
/// Форма логина: длина и «не пусто». Состав символов проверяет домен
/// (User.NormalizeLogin) — там же, где нормализация, чтобы правила не
/// разъехались между слоями.
/// </summary>
public sealed class ChangeLoginCommandValidator : AbstractValidator<ChangeLoginCommand>
{
    public ChangeLoginCommandValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty()
            .WithError(Errors.User.LoginRequired())
            .MinimumLength(User.MinLoginLength)
            .WithError(Errors.User.LoginTooShort(User.MinLoginLength))
            .MaximumLength(User.MaxLoginLength)
            .WithError(Errors.User.LoginTooLong(User.MaxLoginLength));
    }
}
