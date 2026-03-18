using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Deceased;

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

        if (birthDate.HasValue && birthDate.Value.Date > deathDate.Date)
            return Errors.LifePeriod.BirthDateAfterDeathDate();

        return Result.Success<LifePeriod, Error>(new LifePeriod(
            birthDate?.Date,
            deathDate.Date));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BirthDate?.Date ?? DateTime.MinValue;
        yield return DeathDate.Date;
    }
}