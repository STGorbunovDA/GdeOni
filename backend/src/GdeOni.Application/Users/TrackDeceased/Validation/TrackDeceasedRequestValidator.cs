using FluentValidation;
using GdeOni.Application.Users.TrackDeceased.Model;

namespace GdeOni.Application.Users.TrackDeceased.Validation;

public sealed class TrackDeceasedRequestValidator : AbstractValidator<TrackDeceasedRequest>
{
    public TrackDeceasedRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DeceasedId).NotEmpty();

        RuleFor(x => x.PersonalNotes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.PersonalNotes));
    }
}