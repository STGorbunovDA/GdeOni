namespace GdeOni.Infrastructure.Notifications.Push;

/// <summary>
/// Подписка браузера на push-уведомления. Инфра-сущность, не доменный
/// агрегат: доменных инвариантов нет, это просто адрес доставки, выданный
/// push-сервисом браузера (FCM/Mozilla/…).
///
/// У одного пользователя записей столько, со скольких устройств он включил
/// уведомления. Ключ уникальности — endpoint (он и есть адрес устройства).
/// </summary>
internal sealed class PushSubscriptionRecord
{
    public const int MaxEndpointLength = 1000;
    public const int MaxKeyLength = 200;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>URL push-сервиса браузера — куда слать сообщение.</summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>Публичный ключ клиента для шифрования payload.</summary>
    public string P256dh { get; set; } = null!;

    /// <summary>Секрет аутентификации, тоже от браузера.</summary>
    public string Auth { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Когда последний раз успешно доставили. Нужен, чтобы понимать живость
    /// подписки; протухшие удаляем по 404/410 от push-сервиса.
    /// </summary>
    public DateTime? LastSuccessAtUtc { get; set; }
}
