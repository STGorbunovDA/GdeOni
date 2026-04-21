using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateProfile.Validation;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    private const int MaxUserNameLength = 100;
    private const int MaxFullNameLength = 300;

    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.User.IdRequired());

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithError(Errors.User.UserNameRequired())
            .MaximumLength(MaxUserNameLength)
            .WithError(Errors.User.UserNameTooLong(MaxUserNameLength));

        RuleFor(x => x.FullName)
            .MaximumLength(MaxFullNameLength)
            .WithError(Errors.User.FullNameTooLong(MaxFullNameLength))
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));
    }
}