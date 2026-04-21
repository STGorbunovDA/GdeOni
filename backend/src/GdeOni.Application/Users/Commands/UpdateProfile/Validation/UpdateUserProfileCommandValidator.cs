using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateProfile.Validation;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.User.IdRequired());

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithError(Errors.User.UserNameRequired())
            .MaximumLength(User.MaxUserNameLength)
            .WithError(Errors.User.UserNameTooLong(User.MaxUserNameLength));

        RuleFor(x => x.FullName)
            .MaximumLength(User.MaxFullNameLength)
            .WithError(Errors.User.FullNameTooLong(User.MaxFullNameLength))
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));
    }
}