namespace GdeOni.Application.Common.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Фиксированный валидный hash для выравнивания времени ответа login,
    /// когда пользователь не найден (защита от timing-based user enumeration).
    /// Verify(any-password, DummyHash) всегда даёт false и тратит то же
    /// CPU-время, что и Verify против настоящего пользовательского хеша.
    /// </summary>
    string DummyHash { get; }
}