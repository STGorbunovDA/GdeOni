using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.ConfirmEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ConfirmEmail.Validation;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    /// <summary>
    /// Верхний предел длины токена: свой мы генерируем 32-байтным
    /// (43 символа base64url), запас берём с большим походом. Нужен,
    /// чтобы гигантская строка не доезжала до SHA-256 и запроса в БД.
    /// </summary>
    private const int MaxTokenLength = 512;

    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithError(Errors.User.EmailConfirmationTokenInvalid())
            .MaximumLength(MaxTokenLength)
            .WithError(Errors.User.EmailConfirmationTokenInvalid());
    }
}
