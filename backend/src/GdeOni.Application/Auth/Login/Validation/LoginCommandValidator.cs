using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Login.Validation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    private const int MaxEmailLength = 320;

    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithError(Errors.User.EmailRequired())
            .EmailAddress()
            .WithError(Errors.User.EmailInvalid())
            .MaximumLength(MaxEmailLength)
            .WithError(Errors.User.EmailTooLong(MaxEmailLength));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(Errors.User.PasswordRequired());
    }
}