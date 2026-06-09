using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Support;

/// <summary>
/// Универсальный тикет службы поддержки. Два источника:
/// <see cref="SupportTicketSource.Manual"/> — юзер заполнил форму
/// "Обращение в поддержку"; <see cref="SupportTicketSource.Auto"/> —
/// бек сам обнаружил аномалию (например, webhook от YooKassa с
/// неизвестным external_payment_id).
///
/// <para>
/// <c>UserId</c> nullable — для auto-инцидентов, где привязка к
/// конкретному юзеру невозможна (платёж пришёл, а в БД его нет, значит
/// и user_id мы не знаем).
/// </para>
///
/// <para>
/// <c>Severity</c> ортогонален <c>Status</c>: тикет может быть
/// одновременно InProgress и Urgent. Это фильтр-измерение, не часть
/// state-machine'ы.
/// </para>
/// </summary>
public sealed class SupportTicket : Entity<Guid>
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 4000;
    public const int MaxResolutionNoteLength = 4000;
    public const int MaxDetailsLength = 8000;
    public const int MaxUserReplyLength = 4000;

    public Guid? UserId { get; private set; }
    public SupportTicketSource Source { get; private set; }
    public SupportTicketKind Kind { get; private set; }
    public SupportTicketSeverity Severity { get; private set; }
    public SupportTicketStatus Status { get; private set; }

    public string Title { get; private set; }
    public string Description { get; private set; }

    /// <summary>
    /// Произвольный JSON-payload с контекстом для auto-инцидентов
    /// (external_payment_id, error code, snapshot БД). Для Manual
    /// обычно null.
    /// </summary>
    public string? Details { get; private set; }

    public string? ResolutionNote { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    /// <summary>
    /// Юзер закрепил решение админа ("Закрепить решено"). Тикет
    /// остаётся Resolved, но получает явное подтверждение со стороны
    /// автора — полезно для метрик и для админа (видно "решено и
    /// согласовано" vs "решено, но юзер не подтвердил").
    /// </summary>
    public bool AcceptedByUser { get; private set; }
    public DateTime? AcceptedByUserAtUtc { get; private set; }

    /// <summary>
    /// Текст, который юзер написал при последнем Reopen ("Продолжить
    /// спор"). Перезатирается на каждом новом reopen. Описание
    /// исходного обращения остаётся в Description.
    /// </summary>
    public string? LastUserReply { get; private set; }
    public DateTime? LastUserReplyAtUtc { get; private set; }

    /// <summary>
    /// Сколько раз юзер переоткрывал тикет. Без жёсткого лимита —
    /// админ сам решит когда эскалировать, видя счётчик.
    /// </summary>
    public int ReopenedCount { get; private set; }

    /// <summary>
    /// D25.2. История переписки. Хронология ASC по CreatedAtUtc.
    /// Первое сообщение — это исходный Description (не дублируется
    /// в Messages), дальше — ответы админа (резолюции) и реплики
    /// юзера (Reopen).
    /// </summary>
    private readonly List<SupportTicketMessage> _messages = new();
    public IReadOnlyCollection<SupportTicketMessage> Messages => _messages.AsReadOnly();

    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private SupportTicket() : base(Guid.Empty)
    {
        Title = null!;
        Description = null!;
    }

    private SupportTicket(
        Guid id,
        Guid? userId,
        SupportTicketSource source,
        SupportTicketKind kind,
        SupportTicketSeverity severity,
        string title,
        string description,
        string? details,
        DateTime createdAtUtc) : base(id)
    {
        UserId = userId;
        Source = source;
        Kind = kind;
        Severity = severity;
        Status = SupportTicketStatus.Open;
        Title = title;
        Description = description;
        Details = details;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Создание Manual-тикета юзером через форму обращения. Severity
    /// по умолчанию Normal — апгрейдить до Urgent может только админ.
    /// </summary>
    public static Result<SupportTicket, Error> CreateManual(
        Guid userId,
        SupportTicketKind kind,
        string title,
        string description,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
            return Errors.General.ValueIsRequired("userId");

        var validated = ValidateContent(kind, title, description);
        if (validated.IsFailure)
            return validated.Error;

        return new SupportTicket(
            Guid.NewGuid(),
            userId,
            SupportTicketSource.Manual,
            kind,
            SupportTicketSeverity.Normal,
            validated.Value.Title,
            validated.Value.Description,
            details: null,
            createdAtUtc: nowUtc);
    }

    /// <summary>
    /// Создание Auto-инцидента бекендом. UserId nullable — для случаев,
    /// когда конкретного юзера определить не получается. Severity
    /// указывает caller (обычно Urgent для платёжных аномалий).
    /// </summary>
    public static Result<SupportTicket, Error> CreateAuto(
        Guid? userId,
        SupportTicketKind kind,
        SupportTicketSeverity severity,
        string title,
        string description,
        string? details,
        DateTime nowUtc)
    {
        if (userId is { } id && id == Guid.Empty)
            return Errors.General.ValueIsRequired("userId");

        if (!Enum.IsDefined(typeof(SupportTicketSeverity), severity)
            || severity == SupportTicketSeverity.Unknown)
        {
            return Errors.Support.SeverityInvalid();
        }

        var validated = ValidateContent(kind, title, description);
        if (validated.IsFailure)
            return validated.Error;

        if (details is { Length: > MaxDetailsLength })
        {
            return Error.Validation(
                "support_ticket.details.too_long",
                $"Details must not exceed {MaxDetailsLength} characters.");
        }

        return new SupportTicket(
            Guid.NewGuid(),
            userId,
            SupportTicketSource.Auto,
            kind,
            severity,
            validated.Value.Title,
            validated.Value.Description,
            details,
            createdAtUtc: nowUtc);
    }

    /// <summary>
    /// Админ меняет статус. Open ↔ InProgress свободно. Resolve
    /// требует note и фиксирует кто/когда. Повторный Resolve блокируется
    /// AlreadyResolved — чтобы случайным дабл-кликом не перезатереть
    /// предыдущую резолюцию.
    /// </summary>
    public UnitResult<Error> ChangeStatus(
        SupportTicketStatus newStatus,
        Guid adminId,
        string? resolutionNote,
        DateTime nowUtc)
    {
        if (!Enum.IsDefined(typeof(SupportTicketStatus), newStatus)
            || newStatus == SupportTicketStatus.Unknown)
        {
            return Errors.Support.StatusInvalid();
        }

        // No-op: тот же статус — ничего не делаем, не дёргаем
        // UpdatedAtUtc.
        if (newStatus == Status)
            return UnitResult.Success<Error>();

        if (Status == SupportTicketStatus.Resolved)
            return Errors.Support.AlreadyResolved();

        if (newStatus == SupportTicketStatus.Resolved)
        {
            if (string.IsNullOrWhiteSpace(resolutionNote))
                return Errors.Support.ResolutionNoteRequired();

            var trimmedNote = resolutionNote.Trim();
            if (trimmedNote.Length > MaxResolutionNoteLength)
                return Errors.Support.ResolutionNoteTooLong(MaxResolutionNoteLength);

            ResolutionNote = trimmedNote;
            ResolvedByUserId = adminId == Guid.Empty ? null : adminId;
            ResolvedAtUtc = nowUtc;

            // D25.2. Каждый ответ админа = сообщение в переписке.
            // Фабрика SupportTicketMessage.CreateFromAdmin валидирует
            // длину/пустоту — но trimmedNote уже проверен выше, повтор
            // ловит её на уровне domain-инварианта.
            var msg = SupportTicketMessage.CreateFromAdmin(Id, adminId, trimmedNote, nowUtc);
            if (msg.IsFailure) return msg.Error;
            _messages.Add(msg.Value);
        }

        Status = newStatus;
        UpdatedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Админ меняет приоритет. На Resolved-тикете запрещено — после
    /// закрытия severity уже не имеет смысла.
    /// </summary>
    public UnitResult<Error> ChangeSeverity(
        SupportTicketSeverity newSeverity,
        DateTime nowUtc)
    {
        if (!Enum.IsDefined(typeof(SupportTicketSeverity), newSeverity)
            || newSeverity == SupportTicketSeverity.Unknown)
        {
            return Errors.Support.SeverityInvalid();
        }

        if (Status == SupportTicketStatus.Resolved)
            return Errors.Support.AlreadyResolved();

        if (newSeverity == Severity)
            return UnitResult.Success<Error>();

        Severity = newSeverity;
        UpdatedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Юзер закрепил резолюцию ("Закрепить решено"). Доступно только
    /// автору и только если тикет уже Resolved. Идемпотентно: повторный
    /// вызов на уже AcceptedByUser → AlreadyAccepted (юзер не может
    /// случайно дважды "переподтвердить" с разной датой).
    /// </summary>
    public UnitResult<Error> AcceptResolution(Guid actingUserId, DateTime nowUtc)
    {
        if (UserId is null || UserId.Value != actingUserId)
            return Errors.Support.ModifyForbidden();

        if (Status != SupportTicketStatus.Resolved)
            return Errors.Support.AcceptOnlyAfterResolved();

        if (AcceptedByUser)
            return Errors.Support.AlreadyAccepted();

        AcceptedByUser = true;
        AcceptedByUserAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Юзер не согласен с резолюцией ("Продолжить спор"). Тикет
    /// возвращается в Open, ResolutionNote/ResolvedByUserId/ResolvedAtUtc
    /// сохраняются (история не теряется), userReply фиксируется как
    /// LastUserReply, ReopenedCount инкрементится, AcceptedByUser
    /// сбрасывается на false. Доступно только автору и только если
    /// тикет Resolved.
    /// </summary>
    public UnitResult<Error> Reopen(
        Guid actingUserId,
        string? userReply,
        DateTime nowUtc)
    {
        if (UserId is null || UserId.Value != actingUserId)
            return Errors.Support.ModifyForbidden();

        if (Status != SupportTicketStatus.Resolved)
            return Errors.Support.ReopenOnlyAfterResolved();

        // Если юзер уже закрепил "Решено" — спор окончен. Хочет
        // новое — пусть создаёт отдельное обращение.
        if (AcceptedByUser)
            return Errors.Support.AlreadyAccepted();

        string? trimmedReply = null;
        if (!string.IsNullOrWhiteSpace(userReply))
        {
            trimmedReply = userReply.Trim();
            if (trimmedReply.Length > MaxUserReplyLength)
                return Errors.Support.UserReplyTooLong(MaxUserReplyLength);
        }

        Status = SupportTicketStatus.Open;
        AcceptedByUser = false;
        AcceptedByUserAtUtc = null;
        LastUserReply = trimmedReply;
        LastUserReplyAtUtc = trimmedReply is null ? null : nowUtc;
        ReopenedCount++;
        UpdatedAtUtc = nowUtc;

        // D25.2. Если юзер написал текст при Reopen — это сообщение
        // в переписке. Без текста (просто кнопка) сообщение не
        // добавляем — иначе в чате будет пустой пузырь.
        if (trimmedReply is not null)
        {
            var msg = SupportTicketMessage.CreateFromUser(Id, actingUserId, trimmedReply, nowUtc);
            if (msg.IsFailure) return msg.Error;
            _messages.Add(msg.Value);
        }

        return UnitResult.Success<Error>();
    }

    private static Result<(string Title, string Description), Error> ValidateContent(
        SupportTicketKind kind,
        string title,
        string description)
    {
        if (!Enum.IsDefined(typeof(SupportTicketKind), kind)
            || kind == SupportTicketKind.Unknown)
        {
            return Errors.Support.KindInvalid();
        }

        if (string.IsNullOrWhiteSpace(title))
            return Errors.Support.TitleRequired();

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > MaxTitleLength)
            return Errors.Support.TitleTooLong(MaxTitleLength);

        if (string.IsNullOrWhiteSpace(description))
            return Errors.Support.DescriptionRequired();

        var trimmedDescription = description.Trim();
        if (trimmedDescription.Length > MaxDescriptionLength)
            return Errors.Support.DescriptionTooLong(MaxDescriptionLength);

        return (trimmedTitle, trimmedDescription);
    }
}
