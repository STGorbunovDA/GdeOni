using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Constants;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Register.Validation;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private const int MaxEmailLength = 320;
    private const int MaxUserNameLength = 100;
    private const int MaxFullNameLength = 300;

    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithError(Errors.User.EmailRequired())
            .EmailAddress()
            .WithError(Errors.User.EmailInvalid())
            .MaximumLength(MaxEmailLength)
            .WithError(Errors.User.EmailTooLong(MaxEmailLength));

        RuleFor(x => x.UserName)
            .MaximumLength(MaxUserNameLength)
            .WithError(Errors.User.UserNameTooLong(MaxUserNameLength))
            .When(x => !string.IsNullOrWhiteSpace(x.UserName));

        RuleFor(x => x.FullName)
            .MaximumLength(MaxFullNameLength)
            .WithError(Errors.User.FullNameTooLong(MaxFullNameLength))
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(Errors.User.PasswordRequired())
            .MinimumLength(PasswordPolicy.MinPasswordLength)
            .WithError(Errors.User.PasswordTooShort(PasswordPolicy.MinPasswordLength));
    }
}