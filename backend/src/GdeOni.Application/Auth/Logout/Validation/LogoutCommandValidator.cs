using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Logout.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Logout.Validation;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithError(Errors.RefreshToken.TokenRequired());
    }
}
