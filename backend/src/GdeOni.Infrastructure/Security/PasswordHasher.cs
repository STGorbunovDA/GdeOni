using GdeOni.Application.Common.Security;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly int _workFactor;
    private readonly Lazy<string> _dummyHashLazy;

    public PasswordHasher(IOptions<BCryptOptions> options)
    {
        _workFactor = options.Value.WorkFactor;
        // Lazy → BCrypt.HashPassword вычисляется ровно один раз на процесс
        // при первом обращении. Дальше — копеечный геттер.
        _dummyHashLazy = new Lazy<string>(
            () => BCrypt.Net.BCrypt.HashPassword(
                "dummy-not-real-password-for-timing-uniformity",
                _workFactor),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string DummyHash => _dummyHashLazy.Value;

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
