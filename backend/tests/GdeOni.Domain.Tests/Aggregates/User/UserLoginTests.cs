using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// Логин — уникальный идентификатор для входа (в отличие от UserName, где
/// тёзки допустимы). По умолчанию строится из части email до «@».
/// Правила зеркалит backfill в миграции AddUserLogin — если меняешь их
/// здесь, правь и SQL там.
/// </summary>
public sealed class UserLoginTests
{
    private const string SamplePasswordHash = "hash$with$enough$chars";

    private static User NewUser(string email) =>
        User.Register(email: email, passwordHash: SamplePasswordHash).Value;

    [Fact]
    public void Register_GeneratesLoginFromEmailPrefix()
    {
        NewUser("ots4@yandex.ru").Login.Should().Be("ots4");
    }

    [Fact]
    public void Register_LoginIsLowercased()
    {
        NewUser("Petr.Ivanov@MAIL.ru").Login.Should().Be("petr.ivanov");
    }

    [Fact]
    public void Register_DropsCharactersNotAllowedInLogin()
    {
        // «+tag» — распространённый приём почтовых алиасов; в логине плюса быть
        // не должно, иначе его не набрать в поле входа.
        NewUser("ivan+tag@mail.ru").Login.Should().Be("ivantag");
    }

    [Fact]
    public void Register_PadsTooShortPrefix()
    {
        // «ab» короче MinLoginLength — иначе логин не прошёл бы собственную
        // же валидацию длины.
        NewUser("ab@mail.ru").Login.Should().Be("ab0");
    }

    [Fact]
    public void Register_FallsBackToUserWhenPrefixHasNoUsableChars()
    {
        NewUser("___@mail.ru").Login.Should().Be("user");
    }

    [Fact]
    public void Register_UsesExplicitLoginWhenProvided()
    {
        // Use case подбирает свободный логин (ivan → ivan2) и передаёт его сюда.
        var user = User.Register(
            email: "ivan@yandex.ru",
            passwordHash: SamplePasswordHash,
            birthDate: new DateOnly(1990, 1, 1),
            nowUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            login: "ivan2");

        user.Value.Login.Should().Be("ivan2");
    }

    [Fact]
    public void Register_RejectsInvalidExplicitLogin()
    {
        var user = User.Register(
            email: "ivan@yandex.ru",
            passwordHash: SamplePasswordHash,
            birthDate: new DateOnly(1990, 1, 1),
            nowUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            login: "Иван Петров");

        user.IsFailure.Should().BeTrue();
        user.Error.Code.Should().Be("user.login.invalid");
    }

    [Theory]
    [InlineData("ivan", "ivan")]
    [InlineData("  IVAN  ", "ivan")]
    [InlineData("ivan.petrov_1-2", "ivan.petrov_1-2")]
    public void NormalizeLogin_AcceptsAndCanonicalizes(string input, string expected)
    {
        var result = User.NormalizeLogin(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "user.login.required")]
    [InlineData("   ", "user.login.required")]
    [InlineData("ab", "user.login.too_short")]
    [InlineData("иван", "user.login.invalid")]
    [InlineData("ivan petrov", "user.login.invalid")]
    [InlineData("ivan@mail", "user.login.invalid")]
    public void NormalizeLogin_RejectsInvalid(string input, string expectedCode)
    {
        var result = User.NormalizeLogin(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void NormalizeLogin_RejectsTooLong()
    {
        var result = User.NormalizeLogin(new string('a', User.MaxLoginLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.login.too_long");
    }

    [Fact]
    public void GenerateLoginFromEmail_TruncatesToMaxLength()
    {
        var email = new string('a', User.MaxLoginLength + 50) + "@mail.ru";

        User.GenerateLoginFromEmail(email).Length.Should().Be(User.MaxLoginLength);
    }
}
