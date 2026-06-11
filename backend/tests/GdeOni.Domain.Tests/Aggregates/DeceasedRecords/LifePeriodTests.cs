using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Domain.Tests.Aggregates.DeceasedRecords;

/// <summary>
/// Тесты value object'а <see cref="LifePeriod"/> — пара
/// (BirthDate?, DeathDate). DeathDate обязателен (мы каталогизируем
/// именно умерших), BirthDate опционален. Инварианты:
/// смерть не может быть в будущем; рождение не может быть позже смерти.
/// </summary>
public sealed class LifePeriodTests
{
    /// <summary>
    /// Дата смерти в будущем — заведомо невалидна. Это либо опечатка
    /// (поменяли местами birth/death), либо вредоносный ввод.
    /// Domain ловит как `life_period.death_date.in_future`.
    /// </summary>
    [Fact]
    public void Create_DeathDateInFuture_ReturnsDeathDateInFuture()
    {
        // Arrange: дата на год вперёд от текущей utc-даты.
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        // Act
        var result = LifePeriod.Create(birthDate: null, deathDate: future);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("life_period.death_date.in_future");
    }

    /// <summary>
    /// BirthDate позже DeathDate — нарушение хронологии: человек
    /// не мог родиться после собственной смерти. Domain отвергает
    /// с конкретным `life_period.birth_date.after_death_date`.
    /// </summary>
    [Fact]
    public void Create_BirthDateAfterDeathDate_ReturnsBirthDateAfterDeathDate()
    {
        // Arrange: рождение позже смерти.
        var birth = new DateOnly(2000, 1, 1);
        var death = new DateOnly(1999, 1, 1);

        // Act
        var result = LifePeriod.Create(birthDate: birth, deathDate: death);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("life_period.birth_date.after_death_date");
    }

    /// <summary>
    /// Happy path: корректные даты создают валидный LifePeriod
    /// и считают возраст на момент смерти. Birth = 1950-06-15,
    /// Death = 2010-06-14 — день рождения ещё не наступил, поэтому
    /// возраст 59 (не 60).
    /// </summary>
    [Fact]
    public void AgeAtDeath_BirthdayNotYetReached_ReturnsAgeMinusOne()
    {
        // Arrange
        var birth = new DateOnly(1950, 6, 15);
        var death = new DateOnly(2010, 6, 14);

        // Act
        var result = LifePeriod.Create(birth, death);

        // Assert: классический возрастной расчёт — день рождения
        // в году смерти не наступил, значит возраст 59.
        result.IsSuccess.Should().BeTrue();
        result.Value.AgeAtDeath().Should().Be(59);
    }

    /// <summary>
    /// Без BirthDate возраст не определён — должен возвращаться null.
    /// Это нормальный сценарий: для исторических деятелей дата
    /// рождения часто неизвестна.
    /// </summary>
    [Fact]
    public void AgeAtDeath_WithoutBirthDate_ReturnsNull()
    {
        var death = new DateOnly(1900, 1, 1);
        var result = LifePeriod.Create(birthDate: null, deathDate: death);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgeAtDeath().Should().BeNull();
    }

    /// <summary>
    /// HasBirthDate отражает наличие BirthDate — простая обёртка
    /// над nullable, но тестируем явно, чтобы случайный рефакторинг
    /// (например, на Nullable struct) не сломал семантику.
    /// </summary>
    [Fact]
    public void HasBirthDate_WithBirthDate_ReturnsTrue()
    {
        var period = LifePeriod.Create(new DateOnly(1950, 6, 15), new DateOnly(2010, 1, 1)).Value;
        period.HasBirthDate().Should().BeTrue();
    }

    [Fact]
    public void HasBirthDate_WithoutBirthDate_ReturnsFalse()
    {
        var period = LifePeriod.Create(birthDate: null, deathDate: new DateOnly(2010, 1, 1)).Value;
        period.HasBirthDate().Should().BeFalse();
    }

    /// <summary>
    /// DeathDate == default (DateOnly.MinValue) — это не указанная
    /// дата (forgot to fill). Domain отвергает с DeathDateRequired,
    /// иначе мы бы каталогизировали "умер 0001-01-01" — мусор.
    /// </summary>
    [Fact]
    public void Create_DefaultDeathDate_ReturnsDeathDateRequired()
    {
        var result = LifePeriod.Create(birthDate: null, deathDate: default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("life_period.death_date.required");
    }

    /// <summary>
    /// Equality: два LifePeriod без BirthDate равны (тест-страховка
    /// на тот же баг с Nullable, что в BurialLocation.AccuracyMeters —
    /// см. комментарий в LifePeriod.GetEqualityComponents).
    /// </summary>
    [Fact]
    public void Equality_TwoPeriodsWithoutBirthDate_AreEqual()
    {
        var a = LifePeriod.Create(birthDate: null, deathDate: new DateOnly(2010, 1, 1)).Value;
        var b = LifePeriod.Create(birthDate: null, deathDate: new DateOnly(2010, 1, 1)).Value;

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
