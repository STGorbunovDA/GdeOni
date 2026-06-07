namespace GdeOni.API.Options;

/// <summary>
/// Защита webhook-эндпоинтов через IP-whitelist YooKassa.
/// Опционально: если AllowedCidrs пуст — middleware no-op, пропускает
/// весь трафик (полагаемся на pull-verify в payment-provider'е).
/// Актуальный список IP YooKassa:
/// https://yookassa.ru/developers/using-api/webhooks
/// </summary>
public sealed class WebhookSecurityOptions
{
    public const string SectionName = "WebhookSecurity";

    /// <summary>
    /// CIDR-блоки, с которых разрешено принимать webhook'и.
    /// Примеры YooKassa: "185.71.76.0/27", "185.71.77.0/27",
    /// "77.75.153.0/25", "77.75.156.11/32", "77.75.156.35/32",
    /// "2a02:5180:0:1509::/64", "2a02:5180:0:2655::/64",
    /// "2a02:5180:0:1533::/64", "2a02:5180:0:2669::/64".
    /// </summary>
    public string[] AllowedCidrs { get; set; } = Array.Empty<string>();
}
