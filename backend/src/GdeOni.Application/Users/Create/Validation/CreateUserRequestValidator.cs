using FluentValidation;
using GdeOni.Application.Constants;
using GdeOni.Application.Users.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Create.Validation;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.")
            .MaximumLength(320).WithMessage("Email must be at most 320 characters.");

        RuleFor(x => x.UserName)
            .MaximumLength(100).WithMessage("User name must be at most 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.UserName));

        RuleFor(x => x.FullName)
            .MaximumLength(300).WithMessage("Full name must be at most 300 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinPasswordLength)
            .WithMessage($"Password must be at least {PasswordPolicy.MinPasswordLength} characters long.");
    }
}