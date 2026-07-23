using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.ResetPassword.Model;
using GdeOni.Application.Constants;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ResetPassword.Validation;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    /// <summary>
    /// Верхний предел длины токена: свой мы генерируем 32-байтным
    /// (43 символа base64url), запас берём с большим походом. Нужен,
    /// чтобы гигантская строка не доезжала до SHA-256 и запроса в БД.
    /// </summary>
    private const int MaxTokenLength = 512;

    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithError(Errors.User.PasswordResetTokenInvalid())
            .MaximumLength(MaxTokenLength)
            .WithError(Errors.User.PasswordResetTokenInvalid());

        // Здесь, в отличие от login, MinimumLength ставим: это установка
        // НОВОГО пароля, старые «короткие» учётки не пострадают.
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithError(Errors.User.PasswordRequired())
            .MinimumLength(PasswordPolicy.MinPasswordLength)
            .WithError(Errors.User.PasswordTooShort(PasswordPolicy.MinPasswordLength))
            .MaximumLength(PasswordPolicy.MaxPasswordLength)
            .WithError(Errors.User.PasswordTooLong(PasswordPolicy.MaxPasswordLength));
    }
}
