using GdeOni.Application.Anniversaries;

namespace GdeOni.Infrastructure.Notifications;

/// <summary>
/// D37. Лог разосланных писем о годовщинах — механизм дедупликации.
/// Не доменный агрегат, а чисто инфраструктурная запись: гарантирует,
/// что одно письмо на (пользователь, умерший, тип годовщины, дата
/// годовщины) уходит один раз, даже если фоновый сервис прогонится
/// несколько раз за сутки или переживёт рестарт.
///
/// Уникальность обеспечена индексом
/// <see cref="GdeOni.Domain.Shared.DbConstraints.UxSentAnniversaryEmails"/>.
/// </summary>
internal sealed class SentAnniversaryEmail
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid DeceasedId { get; private set; }
    public AnniversaryKind Kind { get; private set; }

    /// <summary>Дата годовщины в локальном часовом поясе сервиса (год наступления).</summary>
    public DateOnly AnniversaryDate { get; private set; }

    public DateTime SentAtUtc { get; private set; }

    private SentAnniversaryEmail() { }

    public static SentAnniversaryEmail Create(
        Guid userId,
        Guid deceasedId,
        AnniversaryKind kind,
        DateOnly anniversaryDate,
        DateTime sentAtUtc) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeceasedId = deceasedId,
            Kind = kind,
            AnniversaryDate = anniversaryDate,
            SentAtUtc = sentAtUtc,
        };
}
