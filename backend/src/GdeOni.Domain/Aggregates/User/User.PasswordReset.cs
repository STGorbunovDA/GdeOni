using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

// Partial-split от User.cs (см. User.Block.cs). D43 — самостоятельное
// восстановление пароля по ссылке из письма.
//
// Почему поля прямо на User, а не отдельная таблица по образцу
// refresh_tokens: активный токен сброса всегда РОВНО ОДИН. Повторный
// запрос перезаписывает предыдущий — это и есть желаемое поведение
// (последнее письмо отменяет предыдущие ссылки). Отдельная таблица
// понадобилась бы только для истории попыток, а её мы не ведём.
public sealed partial class User
{
    /// <summary>
    /// D43. SHA-256 хеш токена сброса пароля. В открытом виде токен живёт
    /// только в письме — в БД его нет, как и у refresh-токенов. Утечка
    /// дампа базы не даёт возможности сбросить чужой пароль.
    /// null — активного запроса на сброс нет.
    /// </summary>
    public string? PasswordResetTokenHash { get; private set; }

    /// <summary>
    /// D43. Момент, после которого ссылка из письма недействительна.
    /// null вместе с <see cref="PasswordResetTokenHash"/>.
    /// </summary>
    public DateTime? PasswordResetTokenExpiresAtUtc { get; private set; }

    /// <summary>
    /// D43. Зарегистрировать запрос на сброс пароля. Хеш и срок приходят
    /// из use case — домен не занимается криптографией и не знает про
    /// конфигурацию TTL.
    ///
    /// Идемпотентности здесь НЕ делаем (в отличие от UpdateProfile и
    /// компании): каждый повторный запрос обязан выдавать новый токен,
    /// иначе «письмо не пришло, жму ещё раз» присылало бы мёртвую ссылку.
    /// SecurityStamp не трогаем — сам факт запроса ещё ничего не меняет
    /// в правах, и разлогинивать человека, который просто нажал
    /// «забыли пароль», незачем.
    /// </summary>
    public UnitResult<Error> RequestPasswordReset(
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            return Errors.User.PasswordResetTokenInvalid();

        if (expiresAtUtc <= nowUtc)
            return Errors.User.PasswordResetTokenExpired();

        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAtUtc = expiresAtUtc;
        Touch();

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D43. Установить новый пароль по токену из письма.
    ///
    /// Проверка самого токена живёт здесь, а не в use case, хотя юзера
    /// уже нашли по хешу: инвариант «пароль меняется только с валидным
    /// непросроченным токеном» принадлежит агрегату, и обходить его
    /// через другой вызов не должно быть возможно.
    ///
    /// После успеха: токен гасится (одноразовость — по ссылке нельзя
    /// пройти дважды) и ротируется SecurityStamp, что закрывает все
    /// активные сессии. Это важно для сценария «аккаунт увели»: настоящий
    /// владелец сбрасывает пароль и тем самым выкидывает злоумышленника.
    /// Ревокация refresh-токенов — забота use case (домен про них не знает).
    /// </summary>
    public UnitResult<Error> ResetPasswordByToken(
        string tokenHash,
        string newPasswordHash,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Errors.User.PasswordHashRequired();

        if (string.IsNullOrWhiteSpace(PasswordResetTokenHash) ||
            PasswordResetTokenExpiresAtUtc is null)
        {
            return Errors.User.PasswordResetTokenInvalid();
        }

        if (!string.Equals(PasswordResetTokenHash, tokenHash, StringComparison.Ordinal))
            return Errors.User.PasswordResetTokenInvalid();

        if (PasswordResetTokenExpiresAtUtc.Value <= nowUtc)
            return Errors.User.PasswordResetTokenExpired();

        PasswordHash = newPasswordHash;
        ClearPasswordResetToken();
        SecurityStamp = Guid.NewGuid();
        Touch();

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D43. Погасить активный запрос на сброс. Вызывается изнутри
    /// агрегата, когда пароль или email меняются обычным путём: если
    /// человек вспомнил пароль и сменил его сам, ранее отправленная
    /// ссылка обязана перестать работать. Иначе старое письмо остаётся
    /// действующим ключом к аккаунту.
    /// </summary>
    private void ClearPasswordResetToken()
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAtUtc = null;
    }
}
