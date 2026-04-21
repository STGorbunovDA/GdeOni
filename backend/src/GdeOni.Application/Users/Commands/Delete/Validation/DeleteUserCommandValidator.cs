using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.Delete.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Delete.Validation;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.User.IdRequired());
    }
}