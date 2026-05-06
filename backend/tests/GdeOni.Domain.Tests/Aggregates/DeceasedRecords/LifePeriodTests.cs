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
    /// с конкретным `life_period.birth_date.invalid`.
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
        result.Error.Code.Should().Be("life_period.birth_date.invalid");
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
}
