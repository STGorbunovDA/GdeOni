using FluentValidation;
using GdeOni.Application.Users.UpdateProfile.Model;

namespace GdeOni.Application.Users.UpdateProfile.Validation;

public sealed class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));
    }
}