using FluentValidation;
using GdeOni.Application.Auth.Logout.Model;

namespace GdeOni.Application.Auth.Logout.Validation;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required");
    }
}
