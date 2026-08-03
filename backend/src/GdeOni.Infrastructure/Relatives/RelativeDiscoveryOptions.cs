namespace GdeOni.Infrastructure.Relatives;

/// <summary>
/// Функция «Родственники» (Фаза 4). Настройки ночного джоба, который ищет
/// новых «родственников». В отличие от email-рассылки, внешних зависимостей
/// (SMTP) у него нет — уведомление внутреннее, поэтому по умолчанию
/// <see cref="Enabled"/> = true.
/// </summary>
public sealed class RelativeDiscoveryOptions
{
    public const string SectionName = "RelativeDiscovery";

    /// <summary>Главный выключатель ночного джоба.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Час локального времени (по <see cref="TimeZoneId"/>), в который раз в
    /// сутки прогоняется поиск новых родственников. 3 — ночью, «вместе с
    /// бэкапом БД», чтобы к утру у человека уже были свежие уведомления.
    /// </summary>
    public int RunAtHourLocal { get; set; } = 3;

    /// <summary>
    /// Часовой пояс для трактовки часа прогона. IANA-id. Дефолт — Москва.
    /// При нераспознанном id сервис падает обратно на UTC (с warning).
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Moscow";

    /// <summary>
    /// Случайный разброс старта (сек), чтобы несколько реплик не начинали
    /// прогон в одну секунду. 0 — без джиттера.
    /// </summary>
    public int MaxJitterSeconds { get; set; } = 120;
}
