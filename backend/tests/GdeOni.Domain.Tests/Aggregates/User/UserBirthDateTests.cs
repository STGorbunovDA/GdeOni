using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Domain.Tests.Aggregates.User;

/// <summary>
/// D19. Возрастной гард <see cref="User.MinAllowedAge"/> = 14 (Условия
/// использования, п. 3.4). Проверки живут в
/// <see cref="User.Register(string, string, DateOnly, DateTime, string?, string?, UserRole)"/>
/// и делегируются на приватный <c>ValidateBirthDate</c>.
/// </summary>
public sealed class UserBirthDateTests
{
    private static readonly DateTime FixedNowUtc =
        new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Ровно 14 лет и один день — регистрируется.</summary>
    [Fact]
    public void Register_JustOverMinAge_Succeeds()
    {
        var birth = DateOnly.FromDateTime(FixedNowUtc).AddYears(-14).AddDays(-1);

        var result = GdeOni.Domain.Aggregates.User.User.Register(
            "teen@example.com",
            "hash",
            birth,
            FixedNowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.BirthDate.Should().Be(birth);
    }

    /// <summary>Ровно 14 лет — регистрируется (граница включительно).</summary>
    [Fact]
    public void Register_ExactlyMinAge_Succeeds()
    {
        var birth = DateOnly.FromDateTime(FixedNowUtc).AddYears(-14);

        var result = GdeOni.Domain.Aggregates.User.User.Register(
            "birthday@example.com",
            "hash",
            birth,
            FixedNowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>Ещё один день до 14-летия — отказ.</summary>
    [Fact]
    public void Register_OneDayBeforeMinAge_Fails()
    {
        var birth = DateOnly.FromDateTime(FixedNowUtc).AddYears(-14).AddDays(1);

        var result = GdeOni.Domain.Aggregates.User.User.Register(
            "toosoon@example.com",
            "hash",
            birth,
            FixedNowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.birth_date.min_age");
    }

    /// <summary>10-летний юзер — отказ.</summary>
    [Fact]
    public void Register_TenYearsOld_Fails()
    {
        var birth = DateOnly.FromDateTime(FixedNowUtc).AddYears(-10);

        var result = GdeOni.Domain.Aggregates.User.User.Register(
            "kid@example.com",
            "hash",
            birth,
            FixedNowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.birth_date.min_age");
    }

    /// <summary>Дата рождения в будущем — отказ (invalid).</summary>
    [Fact]
    public void Register_FutureBirthDate_ReturnsInvalid()
    {
        var birth = DateOnly.FromDateTime(FixedNowUtc).AddDays(1);

        var result = GdeOni.Domain.Aggregates.User.User.Register(
            "future@example.com",
            "hash",
            birth,
            FixedNowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.birth_date.invalid");
    }

    /// <summary>
    /// Legacy-фабрика <c>Register(email, hash, ...)</c> без BirthDate
    /// оставлена для обратной совместимости — создаёт юзера с BirthDate=null.
    /// Использование только для внутренних сценариев (тесты, seed).
    /// </summary>
    [Fact]
    public void LegacyRegister_LeavesBirthDateNull()
    {
        var result = GdeOni.Domain.Aggregates.User.User.Register(
            "legacy@example.com",
            "hash");

        result.IsSuccess.Should().BeTrue();
        result.Value.BirthDate.Should().BeNull();
    }
}
