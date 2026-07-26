using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Commands.UpdateTracking.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateTracking.Validation;

public sealed class UpdateTrackingCommandValidator : AbstractValidator<UpdateTrackingCommand>
{
    public UpdateTrackingCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Tracking.DeceasedIdRequired());

        RuleFor(x => x.RelationshipType)
            .IsInEnum()
            .WithError(Errors.Tracking.RelationshipTypeInvalid());

        RuleFor(x => x.PersonalNotes)
            .MaximumLength(TrackedDeceased.MaxPersonalNotesLength)
            .WithError(Errors.Tracking.PersonalNotesTooLong(TrackedDeceased.MaxPersonalNotesLength))
            .When(x => !string.IsNullOrWhiteSpace(x.PersonalNotes));

        // F42. Наборы «за сколько дней» напоминать о годовщинах: только 0/1/3/7.
        RuleFor(x => x.DeathAnniversaryLeadDays)
            .NotNull().WithMessage("Не указан набор напоминаний о годовщине смерти.");
        RuleForEach(x => x.DeathAnniversaryLeadDays)
            .Must(TrackedDeceased.AllowedLeadDays.Contains)
            .WithMessage("Недопустимое значение напоминания (можно 0, 1, 3, 7 дней).");

        RuleFor(x => x.BirthAnniversaryLeadDays)
            .NotNull().WithMessage("Не указан набор напоминаний о годовщине рождения.");
        RuleForEach(x => x.BirthAnniversaryLeadDays)
            .Must(TrackedDeceased.AllowedLeadDays.Contains)
            .WithMessage("Недопустимое значение напоминания (можно 0, 1, 3, 7 дней).");

        RuleFor(x => x.TrackStatus)
            .IsInEnum()
            .WithError(Errors.Tracking.TrackStatusTypeInvalid());
    }
}