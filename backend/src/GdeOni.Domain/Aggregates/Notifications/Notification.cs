using CSharpFunctionalExtensions;

namespace GdeOni.Domain.Aggregates.Notifications;

/// <summary>
/// Внутрисайтовое уведомление одному пользователю (получатель — Recipient).
/// Заводится сервером в ключевых событиях (новое обращение/жалоба → админам;
/// ответ/решение админа → пользователю) и показывается «колокольчиком» в шапке.
///
/// Фан-аут «одно событие → многим админам» делается на уровне сервиса: на
/// каждого получателя своя запись (у каждого свой флаг прочтения). Поля
/// сан итизируются в <see cref="Create"/>, поэтому фабрика не возвращает Result —
/// доменных ошибок здесь нет (тексты формирует сервер, не пользователь).
/// </summary>
public sealed class Notification : Entity<Guid>
{
    public const int MaxTitleLength = 200;
    public const int MaxBodyLength = 1000;
    public const int MaxLinkLength = 500;

    /// <summary>Кому адресовано уведомление.</summary>
    public Guid RecipientUserId { get; }

    public NotificationKind Kind { get; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>Доп. текст (например заголовок обращения). Может быть null.</summary>
    public string? Body { get; private set; }

    /// <summary>
    /// Относительный путь на клиенте, куда вести по клику (например
    /// <c>/admin/support-tickets/{id}</c>). Может быть null — тогда клик просто
    /// помечает прочитанным.
    /// </summary>
    public string? Link { get; private set; }

    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? ReadAtUtc { get; private set; }

    private Notification() : base(Guid.Empty) { }

    private Notification(
        Guid id,
        Guid recipientUserId,
        NotificationKind kind,
        string title,
        string? body,
        string? link,
        DateTime nowUtc)
        : base(id)
    {
        RecipientUserId = recipientUserId;
        Kind = kind;
        Title = title;
        Body = body;
        Link = link;
        CreatedAtUtc = nowUtc;
        IsRead = false;
    }

    public static Notification Create(
        Guid recipientUserId,
        NotificationKind kind,
        string title,
        string? body,
        string? link,
        DateTime nowUtc)
    {
        var normalizedTitle = (title ?? string.Empty).Trim();
        if (normalizedTitle.Length == 0)
            normalizedTitle = "Уведомление";
        if (normalizedTitle.Length > MaxTitleLength)
            normalizedTitle = normalizedTitle[..MaxTitleLength];

        var normalizedBody = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        if (normalizedBody is { Length: > MaxBodyLength })
            normalizedBody = normalizedBody[..MaxBodyLength];

        var normalizedLink = string.IsNullOrWhiteSpace(link) ? null : link.Trim();
        if (normalizedLink is { Length: > MaxLinkLength })
            normalizedLink = normalizedLink[..MaxLinkLength];

        return new Notification(
            Guid.NewGuid(),
            recipientUserId,
            kind,
            normalizedTitle,
            normalizedBody,
            normalizedLink,
            nowUtc);
    }

    /// <summary>Пометить прочитанным. Идемпотентно: повтор — no-op.</summary>
    public void MarkRead(DateTime nowUtc)
    {
        if (IsRead)
            return;
        IsRead = true;
        ReadAtUtc = nowUtc;
    }
}
