using FluentValidation;
using GdeOni.Application.Complimentary.Commands.Revoke.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Complimentary.Commands.Revoke.Validation;

public sealed class RevokeComplimentaryAccessCommandValidator
    : AbstractValidator<RevokeComplimentaryAccessCommand>
{
    public RevokeComplimentaryAccessCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(Errors.User.IdRequired().Code)
            .WithMessage(Errors.User.IdRequired().Message);
    }
}
