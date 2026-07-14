namespace GdeOni.API.Models.App;

/// <summary>
/// Ответ <c>GET /api/app/features</c>. Решение 2026-05-14: per-feature
/// gating отсутствует — подписка единая на всё приложение.
///
/// D36 (2026-06-12): добавлен <see cref="MediaBaseUrl"/>. Клиенты сами
/// строят media-URL через <c>${MediaBaseUrl}/${bucket}/${encodeURIComponent(key)}</c>,
/// потому что хост MinIO/CDN различается для web (localhost), Android-
/// эмулятора (10.0.2.2) и production (CDN-домен).
/// </summary>
/// <remarks>
/// F39 (2026-07-12): добавлен <c>MonthlyPriceRub</c>. Раньше клиенты
/// писали цену текстом («49 ₽/мес») в пяти местах, а списывалась сумма
/// из <c>SubscriptionOptions</c> — при смене тарифа на кнопке осталась бы
/// одна сумма, а с карты ушла бы другая.
/// </remarks>
public sealed record AppFeaturesResponse(
    bool SubscriptionEnabled,
    int GracePeriodDaysAfterExpiry,
    string MediaBaseUrl,
    decimal MonthlyPriceRub);
