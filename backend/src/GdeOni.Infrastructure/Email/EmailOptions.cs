namespace GdeOni.Infrastructure.Email;

/// <summary>
/// D37. Настройки SMTP-канала. Если <see cref="Host"/> или
/// <see cref="FromEmail"/> пусты — DI регистрирует no-op отправитель, и
/// письма физически не уходят (dev / integration-тесты без почтового
/// сервера).
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP-хост (например, smtp.yandex.ru). Пусто = канал выключен.</summary>
    public string? Host { get; set; }

    /// <summary>Порт SMTP. 587 — STARTTLS (типовой), 465 — implicit SSL, 25 — без шифрования.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Использовать TLS (EnableSsl). Для 587/465 — true.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Логин SMTP (обычно совпадает с FromEmail). Пусто = без аутентификации.</summary>
    public string? UserName { get; set; }

    /// <summary>Пароль/токен приложения SMTP. Секрет — только в локальном конфиге / env.</summary>
    public string? Password { get; set; }

    /// <summary>Адрес в поле From. Обязателен для включения канала.</summary>
    public string? FromEmail { get; set; }

    /// <summary>Отображаемое имя отправителя.</summary>
    public string FromName { get; set; } = "Где Они";

    /// <summary>Таймаут отправки одного письма, секунд.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Канал считается сконфигурированным, если задан хост и адрес
    /// отправителя. Без них SMTP-клиент не поднять.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromEmail);
}
