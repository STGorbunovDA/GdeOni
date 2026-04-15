using FluentValidation;
using GdeOni.Application.Users.Commands.Delete.Model;

namespace GdeOni.Application.Users.Commands.Delete.Validation;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}