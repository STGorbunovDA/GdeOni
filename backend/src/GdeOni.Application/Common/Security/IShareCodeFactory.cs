namespace GdeOni.Application.Common.Security;

/// <summary>
/// D46. Генератор короткого url-safe кода для ссылки «поделиться подборкой»
/// (<c>/s/{code}</c>). Короткий — чтобы QR оставался мелким. Уникальность
/// подстраховывается unique-индексом в БД (use case проверяет коллизию и
/// перегенерирует).
/// </summary>
public interface IShareCodeFactory
{
    /// <summary>Новый случайный код (например, 12 символов base62).</summary>
    string Generate();
}
