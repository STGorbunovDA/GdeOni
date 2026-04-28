using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Login.Model;
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

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(Errors.User.PasswordRequired());
    }
}