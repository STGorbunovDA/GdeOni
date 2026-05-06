using GdeOni.Infrastructure.Security;

namespace GdeOni.Infrastructure.Tests.Security;

/// <summary>
/// Тесты <see cref="PasswordHasher"/> — обёртка над BCrypt.Net-Next.
/// Проверяем три инварианта: Hash+Verify round-trip, Verify на
/// неправильном пароле = false, и каждый Hash для одного пароля
/// даёт разные результаты (рандомная соль). Без Docker — pure CPU.
/// </summary>
public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    /// <summary>
    /// Hash затем Verify тем же паролем = true. Базовый round-trip
    /// сценарий, на котором держится Login.
    /// </summary>
    [Fact]
    public void HashAndVerify_RoundTrip_ReturnsTrue()
    {
        const string password = "Password123!";
        var hash = _hasher.Hash(password);

        _hasher.Verify(password, hash).Should().BeTrue();
    }

    /// <summary>
    /// Verify на неправильном пароле = false. Защищает от багов
    /// "verify всегда true" (был такой реальный CVE в одной из
    /// библиотек).
    /// </summary>
    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Password123!");

        _hasher.Verify("WrongPassword456!", hash).Should().BeFalse();
    }

    /// <summary>
    /// Hash для одного и того же пароля даёт разные хеши — потому
    /// что соль генерируется случайно при каждом Hash. Без рандомной
    /// соли два пользователя с одинаковыми паролями имели бы
    /// одинаковые хеши, и компрометация одного раскрывала бы пароль
    /// у всех.
    /// </summary>
    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        const string password = "Password123!";
        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        hash1.Should().NotBe(hash2);
        // Но оба должны проверяться против исходного пароля.
        _hasher.Verify(password, hash1).Should().BeTrue();
        _hasher.Verify(password, hash2).Should().BeTrue();
    }

    /// <summary>
    /// DummyHash — фиксированный валидный BCrypt-хеш, используется
    /// LoginUseCase'ом для timing-safe защиты от user enumeration.
    /// Verify(any-password, DummyHash) всегда false (мы не знаем
    /// исходного пароля dummy), но тратит то же CPU-время.
    /// Кеширование Lazy: повторное обращение возвращает тот же
    /// инстанс хеша.
    /// </summary>
    [Fact]
    public void DummyHash_AlwaysReturnsSameValue()
    {
        var first = _hasher.DummyHash;
        var second = _hasher.DummyHash;

        first.Should().Be(second);
        first.Should().NotBeNullOrWhiteSpace();
        // Verify против произвольного пароля — false (это и есть
        // суть DummyHash: мы не знаем исходного пароля).
        _hasher.Verify("any-password", first).Should().BeFalse();
    }
}
