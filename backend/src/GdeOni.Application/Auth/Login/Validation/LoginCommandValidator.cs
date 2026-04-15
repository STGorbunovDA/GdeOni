using FluentValidation;
using GdeOni.Application.Auth.Login.Model;

namespace GdeOni.Application.Auth.Login.Validation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}