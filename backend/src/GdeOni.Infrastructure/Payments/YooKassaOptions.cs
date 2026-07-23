namespace GdeOni.Infrastructure.Payments;

/// <summary>
/// D16. Конфигурация интеграции с YooKassa. Биндится из секции
/// <c>YooKassa</c> в appsettings.
/// Если <see cref="SecretKey"/> пустой — DI регистрирует
/// <see cref="FakePaymentProvider"/>; иначе — <see cref="YooKassaPaymentProvider"/>.
/// </summary>
public sealed class YooKassaOptions
{
    public const string SectionName = "YooKassa";

    /// <summary>
    /// Базовый URL API. По умолчанию production endpoint. Для прогона
    /// против sandbox / mock-сервера можно переопределить — например,
    /// в интеграционных тестах через WireMock.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.yookassa.ru";

    /// <summary>
    /// ShopId магазина в YooKassa. Не секрет — допустимо коммитить
    /// в appsettings. Для тестового магазина — "1359063".
    /// </summary>
    public string ShopId { get; set; } = string.Empty;

    /// <summary>
    /// Секретный ключ. КРИТИЧНО: НЕ коммитить в git, хранить только
    /// в локальном appsettings.Development.json (он в .gitignore)
    /// или через env var <c>YooKassa__SecretKey</c>.
    /// Test-ключи начинаются с "test_", боевые — с "live_".
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Включён ли YooKassa-провайдер. true когда <see cref="SecretKey"/>
    /// и <see cref="ShopId"/> заданы. Используется в
    /// <see cref="GdeOni.Infrastructure.DependencyInjection"/> для
    /// выбора между YooKassa и Fake.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(ShopId);

    /// <summary>
    /// D44. Можно ли РЕАЛЬНО оплатить — то есть настроен боевой ключ
    /// (<c>live_*</c>), а не тестовый. Отдаётся клиентам как
    /// <c>PaymentsAvailable</c>: по нему они решают, показывать кнопку
    /// оплаты или вести человека в поддержку (оплата переводом).
    ///
    /// Почему НЕ <see cref="IsConfigured"/>. Тестовый ключ
    /// (<c>test_*</c>) — это рабочая интеграция, но деньги по ней не
    /// приходят. Для пользователя это ровно то же, что «оплата не
    /// работает», поэтому кнопку показывать нельзя. Заодно это страхует
    /// от худшего сценария: тестовые ключи случайно уехали на прод,
    /// человек «оплачивает», деньги не приходят, а он считает, что
    /// заплатил.
    ///
    /// <see cref="IsConfigured"/> при этом не трогаем — он про выбор
    /// провайдера в DI, и на тестовых ключах YooKassa-провайдер должен
    /// работать (иначе не проверить интеграцию до боевых ключей).
    /// </summary>
    public bool IsLivePaymentsEnabled =>
        IsConfigured
        && SecretKey.StartsWith("live_", StringComparison.OrdinalIgnoreCase);
}
