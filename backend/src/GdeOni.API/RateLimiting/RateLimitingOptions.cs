namespace GdeOni.API.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public AuthRateLimitOptions Auth { get; set; } = new();
    public WebhookRateLimitOptions Webhook { get; set; } = new();
}

public sealed class AuthRateLimitOptions
{
    /// <summary>
    /// Имя политики, которое крепится к контроллер-action'ам через
    /// [EnableRateLimiting]. Не меняй без обновления атрибутов.
    /// </summary>
    public const string PolicyName = "auth";

    /// <summary>
    /// Сколько запросов разрешено в окне на одного клиента (IP).
    /// </summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Длина окна в минутах.
    /// </summary>
    public int WindowMinutes { get; set; } = 1;

    /// <summary>
    /// Количество сегментов в sliding window. Чем больше, тем
    /// плавнее «амортизация» лимита по времени. 6 — стандарт.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 6;
}

/// <summary>
/// Лимит для webhook-эндпоинтов (платёжные провайдеры). Эндпоинт
/// AllowAnonymous, без него атакующий мог бы бомбить webhook
/// валидным payload'ом — каждый запрос пишет в БД, отправляет
/// pull-verify к провайдеру. 60/мин per-IP — щедрый потолок для
/// retry-механики YooKassa.
/// </summary>
public sealed class WebhookRateLimitOptions
{
    public const string PolicyName = "webhook";

    public int PermitLimit { get; set; } = 60;
    public int WindowMinutes { get; set; } = 1;
    public int SegmentsPerWindow { get; set; } = 6;
}
