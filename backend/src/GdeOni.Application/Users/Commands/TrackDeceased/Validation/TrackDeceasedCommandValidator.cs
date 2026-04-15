using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.TrackDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.TrackDeceased.Validation;

public sealed class TrackDeceasedCommandValidator : AbstractValidator<TrackDeceasedCommand>
{
    private const int MaxPersonalNotesLength = 2000;

    public TrackDeceasedCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(Errors.Tracking.UserIdRequired());

        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Tracking.DeceasedIdRequired());

        RuleFor(x => x.RelationshipType)
            .IsInEnum()
            .WithError(Errors.Tracking.RelationshipTypeInvalid());

        RuleFor(x => x.PersonalNotes)
            .MaximumLength(MaxPersonalNotesLength)
            .WithError(Errors.Tracking.PersonalNotesTooLong(MaxPersonalNotesLength))
            .When(x => !string.IsNullOrWhiteSpace(x.PersonalNotes));
    }
}