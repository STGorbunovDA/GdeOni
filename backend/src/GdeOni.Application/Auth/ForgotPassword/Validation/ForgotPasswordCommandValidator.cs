using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.ForgotPassword.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ForgotPassword.Validation;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithError(Errors.User.EmailRequired())
            .EmailAddress()
            .WithError(Errors.User.EmailInvalid())
            .MaximumLength(User.MaxEmailLength)
            .WithError(Errors.User.EmailTooLong(User.MaxEmailLength));
    }
}
