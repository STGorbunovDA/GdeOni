namespace GdeOni.Application.Common.Security;

/// <summary>
/// D43. Генератор криптостойких одноразовых токенов общего назначения:
/// <see cref="Generate"/> отдаёт секрет для пользователя, <see cref="Hash"/> —
/// то, что кладём в БД.
///
/// Выделен из <see cref="IRefreshTokenFactory"/>, потому что ровно та же
/// пара операций нужна ссылке восстановления пароля. Реализация одна и та
/// же (32 случайных байта + SHA-256), дублировать криптографию ради
/// второго сценария не стали, а звать «RefreshTokenFactory» из сброса
/// пароля было бы враньём в имени.
/// </summary>
public interface ISecureTokenFactory
{
    /// <summary>Новый случайный токен в открытом виде (url-safe).</summary>
    string Generate();

    /// <summary>Хеш токена — только он попадает в базу.</summary>
    string Hash(string token);
}
