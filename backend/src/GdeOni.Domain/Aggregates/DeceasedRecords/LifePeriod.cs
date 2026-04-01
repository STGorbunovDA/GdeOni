using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class LifePeriod : ValueObject
{
    public DateTime? BirthDate { get; }
    public DateTime DeathDate { get; }

    private LifePeriod() { }

    private LifePeriod(DateTime? birthDate, DateTime deathDate)
    {
        BirthDate = birthDate;
        DeathDate = deathDate;
    }

    public static Result<LifePeriod, Error> Create(DateTime? birthDate, DateTime deathDate)
    {
        if (deathDate == default)
            return Errors.LifePeriod.DeathDateRequired();

        if (deathDate.Date > DateTime.UtcNow.Date)
            return Errors.LifePeriod.DeathDateInFuture();

        if (birthDate.HasValue && birthDate.Value.Date > deathDate.Date)
            return Errors.LifePeriod.BirthDateAfterDeathDate();

        return Result.Success<LifePeriod, Error>(new LifePeriod(
            birthDate?.Date,
            deathDate.Date));
    }

    public bool HasBirthDate() => BirthDate.HasValue;

    public int? AgeAtDeath()
    {
        if (!BirthDate.HasValue)
            return null;

        var age = DeathDate.Year - BirthDate.Value.Year;

        if (BirthDate.Value.Date > DeathDate.AddYears(-age).Date)
            age--;

        return age;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BirthDate?.Date ?? DateTime.MinValue;
        yield return DeathDate.Date;
    }
}