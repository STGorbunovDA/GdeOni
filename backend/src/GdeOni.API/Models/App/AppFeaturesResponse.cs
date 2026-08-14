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
/// <remarks>
/// D44 (2026-07-23): добавлен <c>PaymentsAvailable</c> — настроен ли
/// НАСТОЯЩИЙ платёжный провайдер. Без него в DI подставляется
/// <c>FakePaymentProvider</c>, чей checkout-URL ведёт на
/// <c>example.invalid</c>: кнопка «Оформить подписку» уводила юзера
/// на мёртвую страницу. Клиенты по этому флагу гасят кнопку оплаты
/// и предлагают написать обращение (оплата переводом).
/// </remarks>
public sealed record AppFeaturesResponse(
    bool SubscriptionEnabled,
    int GracePeriodDaysAfterExpiry,
    string MediaBaseUrl,
    decimal MonthlyPriceRub,
    bool PaymentsAvailable,
    // Окно сбора GPS-координат (сек) для веба — из секции Geolocation
    // appsettings. Меняется без пересборки фронта: правишь конфиг → рестарт.
    int GeoAcquireWindowSeconds,
    // Порог ранней остановки сбора координат, метры (из той же секции).
    double GeoTargetAccuracyMeters,
    // Публичный VAPID-ключ для подписки браузера на push. Пустая строка —
    // push на сервере не настроен, клиент прячет переключатель. Ключ не
    // секрет: он и предназначен для передачи в браузер.
    string PushPublicKey);
