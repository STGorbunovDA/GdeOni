using FluentValidation;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;

namespace GdeOni.Application.Users.Commands.ChangeEmail.Validation;

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}