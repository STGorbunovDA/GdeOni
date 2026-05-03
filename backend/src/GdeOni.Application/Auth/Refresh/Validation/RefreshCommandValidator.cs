using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Refresh.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Refresh.Validation;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithError(Errors.RefreshToken.TokenRequired());
    }
}
