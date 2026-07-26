using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

public sealed class TrackedDeceased : Entity<Guid>
{
    public const int MaxPersonalNotesLength = 2000;

    /// <summary>
    /// F42. Допустимые «за сколько дней» напоминания о годовщине: 0 = в день,
    /// 1 = за день, 3 = за 3 дня, 7 = за неделю. Зеркалит набор напоминаний
    /// о праздниках (HolidayReminder).
    /// </summary>
    public static readonly IReadOnlyList<int> AllowedLeadDays = new[] { 0, 1, 3, 7 };

    public Guid DeceasedId { get; }
    public RelationshipType RelationshipType { get; private set; }
    public string? PersonalNotes { get; private set; }

    /// <summary>
    /// F42. Набор «за сколько дней» напоминать о годовщине смерти, как CSV
    /// («0,7»). Пустая строка = напоминание выключено. Хранится строкой, а
    /// не коллекцией — простая колонка без owned-таблицы.
    /// </summary>
    public string DeathAnniversaryLeadDaysCsv { get; private set; } = string.Empty;

    /// <summary>F42. То же для годовщины рождения (дня памяти).</summary>
    public string BirthAnniversaryLeadDaysCsv { get; private set; } = string.Empty;

    /// <summary>Разобранный набор дней напоминания о годовщине смерти.</summary>
    public IReadOnlyList<int> DeathAnniversaryLeadDays => Parse(DeathAnniversaryLeadDaysCsv);

    /// <summary>Разобранный набор дней напоминания о годовщине рождения.</summary>
    public IReadOnlyList<int> BirthAnniversaryLeadDays => Parse(BirthAnniversaryLeadDaysCsv);

    /// <summary>
    /// Обратная совместимость: «напоминать о годовщине смерти» = набор дней не
    /// пуст. Старые клиенты (mobile) читают/пишут булев флаг, домен маппит
    /// его на набор дней (true → «в день», false → выключено).
    /// </summary>
    public bool NotifyOnDeathAnniversary => DeathAnniversaryLeadDays.Count > 0;

    /// <summary>Обратная совместимость: «напоминать о годовщине рождения».</summary>
    public bool NotifyOnBirthAnniversary => BirthAnniversaryLeadDays.Count > 0;

    public TrackStatus Status { get; private set; }
    public DateTime TrackedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private TrackedDeceased() : base(Guid.Empty)
    {
    }

    private TrackedDeceased(
        Guid id,
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes,
        string deathLeadDaysCsv,
        string birthLeadDaysCsv,
        TrackStatus status,
        DateTime trackedAtUtc) : base(id)
    {
        DeceasedId = deceasedId;
        RelationshipType = relationshipType;
        PersonalNotes = personalNotes;
        DeathAnniversaryLeadDaysCsv = deathLeadDaysCsv;
        BirthAnniversaryLeadDaysCsv = birthLeadDaysCsv;
        Status = status;
        TrackedAtUtc = trackedAtUtc;
    }

    public static Result<TrackedDeceased, Error> Create(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes = null,
        bool notifyOnDeathAnniversary = false,
        bool notifyOnBirthAnniversary = false)
    {
        if (deceasedId == Guid.Empty)
            return Errors.Tracking.DeceasedIdRequired();

        if (!Enum.IsDefined(typeof(RelationshipType), relationshipType))
            return Errors.Tracking.RelationshipTypeInvalid();

        var notesResult = NormalizePersonalNotes(personalNotes);
        if (notesResult.IsFailure)
            return notesResult.Error;

        return Result.Success<TrackedDeceased, Error>(
            new TrackedDeceased(
                Guid.NewGuid(),
                deceasedId,
                relationshipType,
                notesResult.Value,
                DefaultCsvFor(notifyOnDeathAnniversary),
                DefaultCsvFor(notifyOnBirthAnniversary),
                TrackStatus.Active,
                DateTime.UtcNow));
    }

    public UnitResult<Error> UpdateRelationship(
        RelationshipType relationshipType,
        string? personalNotes)
    {
        if (!Enum.IsDefined(typeof(RelationshipType), relationshipType))
            return Errors.Tracking.RelationshipTypeInvalid();

        var notesResult = NormalizePersonalNotes(personalNotes);
        if (notesResult.IsFailure)
            return notesResult.Error;

        RelationshipType = relationshipType;
        PersonalNotes = notesResult.Value;
        Touch();

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Обратно-совместимая правка напоминаний булевыми флагами (старые
    /// клиенты). true сохраняет уже заданный набор дней (или ставит «в день»,
    /// если набор был пуст), false — выключает. Так правка со старого клиента
    /// не затирает выбранные на вебе «за неделю/за 3 дня».
    /// </summary>
    public UnitResult<Error> ChangeNotifications(
        bool notifyOnDeathAnniversary,
        bool notifyOnBirthAnniversary)
    {
        var death = notifyOnDeathAnniversary
            ? (DeathAnniversaryLeadDays.Count > 0 ? DeathAnniversaryLeadDays : new[] { 0 })
            : Array.Empty<int>();
        var birth = notifyOnBirthAnniversary
            ? (BirthAnniversaryLeadDays.Count > 0 ? BirthAnniversaryLeadDays : new[] { 0 })
            : Array.Empty<int>();

        return SetAnniversaryReminders(death, birth);
    }

    /// <summary>
    /// F42. Задать наборы «за сколько дней» напоминать о годовщинах смерти и
    /// рождения (значения нормализуются к <see cref="AllowedLeadDays"/>,
    /// дубли/порядок убираются). Пустой набор = выключено. No-op guard: если
    /// оба набора не изменились — без Touch (PATCH тем же не даёт лишний UPDATE).
    /// </summary>
    public UnitResult<Error> SetAnniversaryReminders(
        IReadOnlyList<int> deathLeadDays,
        IReadOnlyList<int> birthLeadDays)
    {
        var death = Serialize(deathLeadDays);
        var birth = Serialize(birthLeadDays);

        if (death == DeathAnniversaryLeadDaysCsv && birth == BirthAnniversaryLeadDaysCsv)
            return UnitResult.Success<Error>();

        DeathAnniversaryLeadDaysCsv = death;
        BirthAnniversaryLeadDaysCsv = birth;
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Archive()
    {
        if (Status == TrackStatus.Archived)
            return Errors.Tracking.AlreadyArchived();

        Status = TrackStatus.Archived;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Mute()
    {
        if (Status == TrackStatus.Muted)
            return Errors.Tracking.AlreadyMuted();

        Status = TrackStatus.Muted;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Activate()
    {
        if (Status == TrackStatus.Active)
            return Errors.Tracking.AlreadyActive();

        Status = TrackStatus.Active;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reactivate(
        RelationshipType relationshipType,
        string? personalNotes,
        bool notifyOnDeathAnniversary,
        bool notifyOnBirthAnniversary)
    {
        var updateRelationshipResult = UpdateRelationship(relationshipType, personalNotes);
        if (updateRelationshipResult.IsFailure)
            return updateRelationshipResult.Error;

        var notificationResult = ChangeNotifications(
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary);

        if (notificationResult.IsFailure)
            return notificationResult.Error;

        Status = TrackStatus.Active;
        Touch();
        return UnitResult.Success<Error>();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsActive() => Status == TrackStatus.Active;
    public bool IsMuted() => Status == TrackStatus.Muted;
    public bool IsArchived() => Status == TrackStatus.Archived;

    public bool HasNotificationsEnabled() =>
        NotifyOnDeathAnniversary || NotifyOnBirthAnniversary;

    private static string DefaultCsvFor(bool notify) =>
        notify ? "0" : string.Empty;

    /// <summary>Нормализует набор к разрешённым дням, убирает дубли, сортирует, склеивает в CSV.</summary>
    private static string Serialize(IReadOnlyList<int>? leadDays)
    {
        if (leadDays is null || leadDays.Count == 0)
            return string.Empty;

        var normalized = leadDays
            .Where(AllowedLeadDays.Contains)
            .Distinct()
            .OrderBy(x => x);

        return string.Join(',', normalized);
    }

    private static IReadOnlyList<int> Parse(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<int>();

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var value) ? value : -1)
            .Where(AllowedLeadDays.Contains)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
    }

    private static Result<string?, Error> NormalizePersonalNotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<string?, Error>(null);

        var normalized = value.Trim();

        if (normalized.Length > MaxPersonalNotesLength)
            return Errors.Tracking.PersonalNotesTooLong(MaxPersonalNotesLength);

        return Result.Success<string?, Error>(normalized);
    }
}
