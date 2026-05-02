using GdeOni.Application.Common.Security;

namespace GdeOni.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    // Lazy → BCrypt.HashPassword вычисляется ровно один раз на процесс
    // при первом обращении. Дальше — копеечный геттер.
    private static readonly Lazy<string> DummyHashLazy = new(
        () => BCrypt.Net.BCrypt.HashPassword("dummy-not-real-password-for-timing-uniformity"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public string DummyHash => DummyHashLazy.Value;

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}