using FluentValidation;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;

namespace GdeOni.Application.Users.Commands.UpdateProfile.Validation;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));
    }
}