using FluentValidation;
using GdeOni.Application.Auth.Refresh.Model;

namespace GdeOni.Application.Auth.Refresh.Validation;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required");
    }
}
