namespace GdeOni.API.RateLimiting;

/// <summary>
/// Настройки rate limiting — биндятся из секции <c>RateLimiting</c>
/// в appsettings.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Имя секции в appsettings, к которой биндятся опции.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Лимит для аутентификационных эндпоинтов (login/register/refresh).</summary>
    public AuthRateLimitOptions Auth { get; set; } = new();
    /// <summary>Лимит для webhook-эндпоинтов платёжных провайдеров.</summary>
    public WebhookRateLimitOptions Webhook { get; set; } = new();
}

/// <summary>
/// Лимит для аутентификационных эндпоинтов: защита от brute-force
/// подбора пароля и спама регистраций.
/// </summary>
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
    /// <summary>Имя политики, крепится к webhook-action'ам через [EnableRateLimiting].</summary>
    public const string PolicyName = "webhook";

    /// <summary>Сколько запросов разрешено в окне на один IP.</summary>
    public int PermitLimit { get; set; } = 60;
    /// <summary>Длина окна в минутах.</summary>
    public int WindowMinutes { get; set; } = 1;
    /// <summary>Количество сегментов в sliding window.</summary>
    public int SegmentsPerWindow { get; set; } = 6;
}
