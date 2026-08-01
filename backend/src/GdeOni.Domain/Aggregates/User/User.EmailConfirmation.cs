using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

// Partial-split от User.cs (см. User.PasswordReset.cs). D45 —
// подтверждение адреса электронной почты по ссылке из письма.
//
// Модель мягкая (решение 2026-07-30): неподтверждённый пользователь
// всё равно входит и пользуется сервисом, но в UI висит баннер
// «Подтвердите email». Поэтому здесь нет ни блокировки логина, ни
// ротации SecurityStamp — подтверждение адреса ничего не меняет в
// правах, а лишь снимает баннер.
//
// Поля живут прямо на User (как у сброса пароля): активный токен
// подтверждения всегда РОВНО ОДИН, повторный запрос («письмо не
// пришло, отправьте ещё раз») перезаписывает предыдущий — последнее
// письмо отменяет ссылки из прежних.
public sealed partial class User
{
    /// <summary>
    /// D45. Подтверждён ли адрес электронной почты. false у новых
    /// регистраций до перехода по ссылке из письма. Исторические
    /// пользователи (до миграции AddEmailConfirmation) проставлены в
    /// true бэкфиллом — их повторно подтверждать не заставляем.
    /// </summary>
    public bool IsEmailConfirmed { get; private set; }

    /// <summary>
    /// D45. Момент подтверждения адреса. null, пока не подтверждён.
    /// </summary>
    public DateTime? EmailConfirmedAtUtc { get; private set; }

    /// <summary>
    /// D45. SHA-256 хеш токена подтверждения. В открытом виде токен
    /// живёт только в письме — в БД его нет, как и у refresh-токенов и
    /// ссылки сброса пароля. null — активного запроса на подтверждение
    /// нет.
    /// </summary>
    public string? EmailConfirmationTokenHash { get; private set; }

    /// <summary>
    /// D45. Момент, после которого ссылка из письма недействительна.
    /// null вместе с <see cref="EmailConfirmationTokenHash"/>.
    /// </summary>
    public DateTime? EmailConfirmationTokenExpiresAtUtc { get; private set; }

    /// <summary>
    /// D45. Подчиняется ли аккаунт жёсткому гейту «вход только после
    /// подтверждения email». true — для новых регистраций (решение
    /// 2026-07-30): пока адрес не подтверждён, <c>LoginUseCase</c> вход
    /// не пускает. false — для «старых» пользователей, зарегистрированных
    /// до фичи (бэкфилл миграции ставит их в false): у них доступ есть,
    /// а неподтверждённость видна лишь баннером.
    ///
    /// Важно: сам гейт в use case дополнительно требует, чтобы почтовый
    /// канал был реально настроен — иначе (dev/тесты без SMTP) флаг никого
    /// не залочит.
    /// </summary>
    public bool EmailConfirmationRequired { get; private set; }

    /// <summary>
    /// D45. Помечает, что аккаунт подчиняется гейту подтверждения email.
    /// Вызывается доменной фабрикой <see cref="Register"/> для новых
    /// регистраций.
    /// </summary>
    internal void MarkEmailConfirmationRequired()
    {
        EmailConfirmationRequired = true;
    }

    /// <summary>
    /// D45. Предподтверждает адрес без токена и письма. Используется
    /// только для сид-сценария (SuperAdmin): его почте доверяем, гонять
    /// через письмо незачем, а под баннер и гейт он попадать не должен.
    /// </summary>
    internal void MarkEmailPreconfirmed(DateTime nowUtc)
    {
        IsEmailConfirmed = true;
        EmailConfirmedAtUtc = nowUtc;
        EmailConfirmationRequired = false;
        ClearEmailConfirmationToken();
    }

    /// <summary>
    /// D45. Зарегистрировать запрос на подтверждение email. Хеш и срок
    /// приходят из use case — домен не занимается криптографией и не
    /// знает про конфигурацию TTL.
    ///
    /// Идемпотентность: если адрес уже подтверждён — no-op Success
    /// (нечего подтверждать, письмо слать незачем; вызывающий по этому
    /// не сможет отличить «уже подтверждён» и корректно ничего не
    /// отправит — см. IEmailConfirmationDispatcher). В остальном НЕ
    /// идемпотентно: каждый повторный запрос выдаёт новый токен, иначе
    /// «письмо не пришло, жму ещё раз» присылало бы мёртвую ссылку.
    /// </summary>
    public UnitResult<Error> RequestEmailConfirmation(
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime nowUtc)
    {
        if (IsEmailConfirmed)
            return UnitResult.Success<Error>();

        if (string.IsNullOrWhiteSpace(tokenHash))
            return Errors.User.EmailConfirmationTokenInvalid();

        if (expiresAtUtc <= nowUtc)
            return Errors.User.EmailConfirmationTokenExpired();

        EmailConfirmationTokenHash = tokenHash;
        EmailConfirmationTokenExpiresAtUtc = expiresAtUtc;
        Touch();

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D45. Подтвердить адрес по токену из письма.
    ///
    /// Проверка токена живёт здесь, а не в use case (хотя юзера уже
    /// нашли по хешу): инвариант «email подтверждается только валидным
    /// непросроченным токеном» принадлежит агрегату.
    ///
    /// Идемпотентность: если адрес уже подтверждён — Success без
    /// изменений. Это закрывает «клик по ссылке дважды»: после первого
    /// подтверждения токен погашен, и повторный клик иначе падал бы
    /// «ссылка недействительна», пугая уже подтвердившегося человека.
    ///
    /// SecurityStamp НЕ ротируется — подтверждение не меняет прав и не
    /// должно разлогинивать активные сессии.
    /// </summary>
    public UnitResult<Error> ConfirmEmailByToken(string tokenHash, DateTime nowUtc)
    {
        if (IsEmailConfirmed)
            return UnitResult.Success<Error>();

        if (string.IsNullOrWhiteSpace(EmailConfirmationTokenHash) ||
            EmailConfirmationTokenExpiresAtUtc is null)
        {
            return Errors.User.EmailConfirmationTokenInvalid();
        }

        if (!string.Equals(EmailConfirmationTokenHash, tokenHash, StringComparison.Ordinal))
            return Errors.User.EmailConfirmationTokenInvalid();

        if (EmailConfirmationTokenExpiresAtUtc.Value <= nowUtc)
            return Errors.User.EmailConfirmationTokenExpired();

        IsEmailConfirmed = true;
        EmailConfirmedAtUtc = nowUtc;
        ClearEmailConfirmationToken();
        Touch();

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D45. Сбрасывает подтверждение в «не подтверждён» и гасит активный
    /// токен. Вызывается изнутри агрегата при смене email обычным путём
    /// (<see cref="ChangeEmail"/>): новый адрес ещё никто не подтверждал,
    /// а старая ссылка вела на прежний ящик и обязана перестать работать.
    /// После этого баннер снова покажется, а resend-эндпоинт вышлет
    /// письмо на новый адрес.
    /// </summary>
    private void ResetEmailConfirmation()
    {
        IsEmailConfirmed = false;
        EmailConfirmedAtUtc = null;
        ClearEmailConfirmationToken();
    }

    private void ClearEmailConfirmationToken()
    {
        EmailConfirmationTokenHash = null;
        EmailConfirmationTokenExpiresAtUtc = null;
    }
}
