using System.Security.Cryptography;
using GdeOni.Application.Common.Security;

namespace GdeOni.Infrastructure.Security;

/// <summary>
/// D46. Короткий base62-код для ссылки «поделиться». 12 символов ≈ 71 бит —
/// подобрать нереально, а QR остаётся мелким. Символы выбираются
/// крипто-стойко и без смещения через
/// <see cref="RandomNumberGenerator.GetItems{T}(ReadOnlySpan{T}, int)"/>.
/// Без состояния → Singleton.
/// </summary>
public sealed class ShareCodeFactory : IShareCodeFactory
{
    private const string Alphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int CodeLength = 12;

    public string Generate() =>
        new(RandomNumberGenerator.GetItems<char>(Alphabet, CodeLength));
}
