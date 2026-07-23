namespace GdeOni.Infrastructure.Notifications;

/// <summary>
/// D37. Настройки фоновой email-рассылки о годовщинах. По умолчанию
/// <see cref="Enabled"/> = false — фича включается осознанно, после
/// того как задан SMTP-канал (секция Email).
/// </summary>
public sealed class AnniversaryEmailOptions
{
    public const string SectionName = "AnniversaryEmails";

    /// <summary>Главный выключатель фоновой рассылки.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Час локального времени (по <see cref="TimeZoneId"/>), в который
    /// раз в сутки уходит рассылка. 9 — утро, письмо ждёт человека к
    /// началу дня.
    /// </summary>
    public int SendAtHourLocal { get; set; } = 9;

    /// <summary>
    /// Часовой пояс для трактовки «сегодня» и часа отправки. IANA-id
    /// (на .NET 8 работает и на Windows через ICU). Дефолт — Москва.
    /// При нераспознанном id сервис падает обратно на UTC (с warning).
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Moscow";

    /// <summary>
    /// Случайный разброс старта рассылки (сек), чтобы несколько реплик
    /// не начинали слать в одну секунду. 0 — без джиттера.
    /// </summary>
    public int MaxJitterSeconds { get; set; } = 120;

    /// <summary>Название сервиса в подписи письма.</summary>
    public string AppName { get; set; } = "Где Они";

    /// <summary>
    /// Базовый URL приложения для кнопки/ссылки в письме (например,
    /// https://gdeoni.ru). null — письмо уходит без ссылки.
    /// </summary>
    public string? AppUrl { get; set; }
}
