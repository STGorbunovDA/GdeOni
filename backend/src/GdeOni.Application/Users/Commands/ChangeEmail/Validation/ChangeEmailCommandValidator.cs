using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.ChangeEmail.Validation;

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.User.IdRequired());

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithError(Errors.User.EmailRequired())
            .EmailAddress()
            .WithError(Errors.User.EmailInvalid());
    }
}