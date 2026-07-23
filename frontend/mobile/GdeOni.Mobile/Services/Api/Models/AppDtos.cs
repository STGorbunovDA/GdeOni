namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// E22. <c>GET /api/app/version</c>. AllowAnonymous на бэке —
/// мобилка дёргает на старте до логина, чтобы поймать force-update
/// даже для протухшего токена.
/// </summary>
public sealed record AppVersionResponse(
    string MinSupportedVersion,
    string LatestVersion,
    string? ForceUpdateMessage,
    string? DownloadUrl);

/// <summary>
/// E22. <c>GET /api/app/features</c>. Кешируется на сессию —
/// определяет, показывать ли paywall и какие фичи доступны.
///
/// D36 (2026-06-12): добавлен <see cref="MediaBaseUrl"/>. Mobile строит
/// URL фото через <c>${MediaBaseUrl}/${bucket}/${encodeURIComponent(key)}</c>.
/// Если бэк ещё старый (нет поля) — клиент использует дефолт
/// <c>http://10.0.2.2:9000</c> для DEBUG (Android-эмулятор) или
/// production-домен в Release.
/// </summary>
/// <remarks>
/// F39. MonthlyPriceRub — цена подписки из конфига бэка, оттуда же её берёт
/// создание платежа. Раньше клиент писал «49 ₽/мес» текстом, и смена тарифа
/// означала бы: на экране одна сумма, а спишется другая.
/// Nullable + дефолт: старый бэк поля не отдаёт — тогда просто не показываем
/// сумму, а не показываем неверную.
/// </remarks>
/// <remarks>
/// D44: <see cref="PaymentsAvailable"/> — подключён ли настоящий
/// платёжный провайдер. Nullable по той же причине, что и цена: старый
/// бэк поля не отдаёт. При null считаем, что оплата НЕДОСТУПНА —
/// показать кнопку оплаты, которая ведёт на заглушку, хуже, чем сразу
/// предложить написать в поддержку.
/// </remarks>
public sealed record AppFeaturesResponse(
    bool SubscriptionEnabled,
    int GracePeriodDaysAfterExpiry,
    string? MediaBaseUrl = null,
    decimal? MonthlyPriceRub = null,
    bool? PaymentsAvailable = null);
