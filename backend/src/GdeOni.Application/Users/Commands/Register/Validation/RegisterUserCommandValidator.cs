using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Constants;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Register.Validation;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithError(Errors.User.EmailRequired())
            .EmailAddress()
            .WithError(Errors.User.EmailInvalid())
            .MaximumLength(User.MaxEmailLength)
            .WithError(Errors.User.EmailTooLong(User.MaxEmailLength));

        RuleFor(x => x.UserName)
            .MaximumLength(User.MaxUserNameLength)
            .WithError(Errors.User.UserNameTooLong(User.MaxUserNameLength))
            .When(x => !string.IsNullOrWhiteSpace(x.UserName));

        RuleFor(x => x.FullName)
            .MaximumLength(User.MaxFullNameLength)
            .WithError(Errors.User.FullNameTooLong(User.MaxFullNameLength))
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(Errors.User.PasswordRequired())
            .MinimumLength(PasswordPolicy.MinPasswordLength)
            .WithError(Errors.User.PasswordTooShort(PasswordPolicy.MinPasswordLength));
    }
}