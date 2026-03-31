using FluentValidation;
using GdeOni.Application.Users.ChangeEmail.Model;

namespace GdeOni.Application.Users.ChangeEmail.Validation;

public sealed class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
{
    public ChangeEmailRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}