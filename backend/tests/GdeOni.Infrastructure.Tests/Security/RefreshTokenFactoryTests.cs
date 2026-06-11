using GdeOni.Infrastructure.Security;

namespace GdeOni.Infrastructure.Tests.Security;

/// <summary>
/// Тесты <see cref="RefreshTokenFactory"/> — генератор refresh-токенов.
/// Generate использует RandomNumberGenerator (cryptographic-grade),
/// Hash — SHA-256 в hex lowercase. Хранится в БД именно hash,
/// чтобы дамп БД не давал прямой доступ к токенам.
/// </summary>
public sealed class RefreshTokenFactoryTests
{
    private readonly RefreshTokenFactory _factory = new();

    /// <summary>
    /// Generate возвращает не-пустую строку base64url-кодированную
    /// (без '+', '/', '=' — URL-safe).
    /// </summary>
    [Fact]
    public void Generate_ReturnsBase64UrlSafeString()
    {
        var token = _factory.Generate();

        token.Should().NotBeNullOrWhiteSpace();
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
        // 32 байта в base64 без padding ≈ 43 символа.
        token.Length.Should().BeGreaterThan(40);
    }

    /// <summary>
    /// Generate каждый раз возвращает разные значения — это
    /// cryptographic random, коллизии практически невозможны.
    /// Защищает от багов "случайно вернули фиксированный seed".
    /// </summary>
    [Fact]
    public void Generate_TwoCalls_ReturnDifferentTokens()
    {
        var first = _factory.Generate();
        var second = _factory.Generate();

        first.Should().NotBe(second);
    }

    /// <summary>
    /// Hash детерминирован: один и тот же input → один и тот же hash.
    /// Это обязательно: при logout/refresh мы Hash'им присланный
    /// токен и ищем по hash в БД. Если бы Hash был недетерминирован
    /// (с солью), поиск был бы невозможен.
    /// </summary>
    [Fact]
    public void Hash_SameInput_ProducesSameHash()
    {
        const string token = "fixed-token-string";

        var first = _factory.Hash(token);
        var second = _factory.Hash(token);

        first.Should().Be(second);
    }

    /// <summary>
    /// Hash возвращает hex-lowercase SHA-256 — ровно 64 символа.
    /// </summary>
    [Fact]
    public void Hash_ReturnsHexLowercase64Chars()
    {
        var hash = _factory.Hash("token");

        hash.Length.Should().Be(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }
}
