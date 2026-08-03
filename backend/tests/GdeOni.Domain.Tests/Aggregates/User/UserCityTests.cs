using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// Город пользователя: указание/очистка через UpdateCity — trim, пустая
/// строка → null, ограничение по длине. У свежего пользователя город пуст.
/// </summary>
public sealed class UserCityTests
{
    private const string SampleEmail = "ivan@example.com";
    private const string SamplePasswordHash = "hash$with$enough$chars";

    private static User NewUser() =>
        User.Register(email: SampleEmail, passwordHash: SamplePasswordHash).Value;

    [Fact]
    public void FreshUser_HasNoCity()
    {
        NewUser().City.Should().BeNull();
    }

    [Fact]
    public void UpdateCity_SetsTrimmedValue()
    {
        var user = NewUser();

        var result = user.UpdateCity("  Москва  ");

        result.IsSuccess.Should().BeTrue();
        user.City.Should().Be("Москва");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateCity_BlankOrNull_ClearsToNull(string? city)
    {
        var user = NewUser();
        user.UpdateCity("Казань");

        var result = user.UpdateCity(city);

        result.IsSuccess.Should().BeTrue();
        user.City.Should().BeNull();
    }

    [Fact]
    public void UpdateCity_TooLong_Rejected()
    {
        var user = NewUser();
        var tooLong = new string('x', User.MaxCityLength + 1);

        var result = user.UpdateCity(tooLong);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.city.too_long");
        user.City.Should().BeNull();
    }

    [Fact]
    public void UpdateCity_SameValue_NoOpSuccess()
    {
        var user = NewUser();
        user.UpdateCity("Москва");

        var result = user.UpdateCity("Москва");

        result.IsSuccess.Should().BeTrue();
        user.City.Should().Be("Москва");
    }
}
