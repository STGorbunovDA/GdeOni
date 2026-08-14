namespace GdeOni.API.Models.Push;

/// <summary>
/// Подписка браузера на push. Все три значения выдаёт сам браузер
/// (PushManager.subscribe) — сервер их не придумывает, только хранит.
/// </summary>
public sealed class PushSubscriptionRequest
{
    /// <summary>URL push-сервиса браузера — адрес конкретного устройства.</summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>Публичный ключ клиента (keys.p256dh) для шифрования payload.</summary>
    public string P256dh { get; set; } = null!;

    /// <summary>Секрет аутентификации (keys.auth).</summary>
    public string Auth { get; set; } = null!;
}

/// <summary>Снятие подписки — достаточно endpoint'а.</summary>
public sealed class PushUnsubscribeRequest
{
    /// <summary>Адрес устройства, который надо забыть.</summary>
    public string Endpoint { get; set; } = null!;
}

/// <summary>Включены ли push хотя бы на одном устройстве.</summary>
public sealed record PushStatusResponse(bool Enabled);
