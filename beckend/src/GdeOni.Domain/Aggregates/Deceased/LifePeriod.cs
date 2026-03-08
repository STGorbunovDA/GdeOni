using CSharpFunctionalExtensions;

namespace GdeOni.Domain.Aggregates.Deceased;

public sealed class LifePeriod : ValueObject
{
    public DateTime? BirthDate { get; }
    public DateTime DeathDate { get; }

    private LifePeriod(DateTime? birthDate, DateTime deathDate)
    {
        BirthDate = birthDate;
        DeathDate = deathDate;
    }

    public static Result<LifePeriod> Create(DateTime? birthDate, DateTime deathDate)
    {
        if (deathDate == default)
            return Result.Failure<LifePeriod>("Дата смерти обязательна");

        if (birthDate.HasValue && birthDate.Value.Date > deathDate.Date)
            return Result.Failure<LifePeriod>("Дата рождения не может быть позже даты смерти");

        return Result.Success(new LifePeriod(
            birthDate?.Date,
            deathDate.Date));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BirthDate?.Date ?? DateTime.MinValue;
        yield return DeathDate.Date;
    }
}