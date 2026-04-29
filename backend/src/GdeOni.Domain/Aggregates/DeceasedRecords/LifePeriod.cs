using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class LifePeriod : ValueObject
{
    public DateOnly? BirthDate { get; }
    public DateOnly DeathDate { get; }

    private LifePeriod() { }

    private LifePeriod(DateOnly? birthDate, DateOnly deathDate)
    {
        BirthDate = birthDate;
        DeathDate = deathDate;
    }

    public static Result<LifePeriod, Error> Create(DateOnly? birthDate, DateOnly deathDate)
    {
        if (deathDate == default)
            return Errors.LifePeriod.DeathDateRequired();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (deathDate > today)
            return Errors.LifePeriod.DeathDateInFuture();

        if (birthDate.HasValue && birthDate.Value > deathDate)
            return Errors.LifePeriod.BirthDateAfterDeathDate();

        return Result.Success<LifePeriod, Error>(new LifePeriod(birthDate, deathDate));
    }

    public bool HasBirthDate() => BirthDate.HasValue;

    public int? AgeAtDeath()
    {
        if (!BirthDate.HasValue)
            return null;

        var birth = BirthDate.Value;
        var age = DeathDate.Year - birth.Year;

        if (birth > DeathDate.AddYears(-age))
            age--;

        return age;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BirthDate ?? DateOnly.MinValue;
        yield return DeathDate;
    }
}
