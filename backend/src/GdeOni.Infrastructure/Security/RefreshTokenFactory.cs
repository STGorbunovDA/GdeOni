using System.Security.Cryptography;
using System.Text;
using GdeOni.Application.Common.Security;

namespace GdeOni.Infrastructure.Security;

public sealed class RefreshTokenFactory : IRefreshTokenFactory
{
    private const int TokenSizeInBytes = 32;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        return Base64UrlEncode(bytes);
    }

    public string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
