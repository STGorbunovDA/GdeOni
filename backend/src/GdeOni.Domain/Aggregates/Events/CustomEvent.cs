using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Events;

/// <summary>
/// Ручное (пользовательское) событие в «Событиях» — например, «ДР друга».
/// Приватное для пользователя. Повторяется каждый год по месяцу/дню
/// <see cref="EventDate"/> (год-якорь хранится для показа, но напоминание
/// срабатывает ежегодно — как праздники и памятные даты).
///
/// <see cref="LeadDaysCsv"/> — набор «за сколько дней напомнить» в CSV
/// («0,1,3,7»): 0 = в день, 1 = за день, 3 = за 3 дня, 7 = за неделю. Пустая
/// строка = напоминание отключено. Зеркалит <see cref="HolidayReminder"/>.
/// </summary>
public sealed class CustomEvent : Entity<Guid>
{
    public const int MaxTitleLength = 200;

    /// <summary>Разрешённые упреждения (в днях): в день, за день, за 3, за неделю.</summary>
    public static readonly IReadOnlySet<int> AllowedLeadDays = new HashSet<int> { 0, 1, 3, 7 };

    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public DateOnly EventDate { get; private set; }
    public string LeadDaysCsv { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<int> LeadDays => Parse(LeadDaysCsv);

    private CustomEvent() : base(Guid.Empty)
    {
        Title = null!;
        LeadDaysCsv = string.Empty;
    }

    private CustomEvent(
        Guid id,
        Guid userId,
        string title,
        DateOnly eventDate,
        string leadDaysCsv,
        DateTime nowUtc) : base(id)
    {
        UserId = userId;
        Title = title;
        EventDate = eventDate;
        LeadDaysCsv = leadDaysCsv;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public static Result<CustomEvent, Error> Create(
        Guid userId,
        string title,
        DateOnly eventDate,
        IReadOnlyCollection<int> leadDays,
        DateTime nowUtc)
    {
        var normalizedTitle = NormalizeTitle(title);
        if (normalizedTitle.IsFailure)
            return normalizedTitle.Error;

        return Result.Success<CustomEvent, Error>(
            new CustomEvent(
                Guid.NewGuid(), userId, normalizedTitle.Value, eventDate,
                Serialize(leadDays), nowUtc));
    }

    /// <summary>
    /// Обновить событие. No-op при структурно тех же значениях (не трогаем
    /// UpdatedAtUtc). Пустой набор дней = напоминание отключено.
    /// </summary>
    public UnitResult<Error> Update(
        string title,
        DateOnly eventDate,
        IReadOnlyCollection<int> leadDays,
        DateTime nowUtc)
    {
        var normalizedTitle = NormalizeTitle(title);
        if (normalizedTitle.IsFailure)
            return normalizedTitle.Error;

        var csv = Serialize(leadDays);
        if (Title == normalizedTitle.Value && EventDate == eventDate && LeadDaysCsv == csv)
            return UnitResult.Success<Error>();

        Title = normalizedTitle.Value;
        EventDate = eventDate;
        LeadDaysCsv = csv;
        UpdatedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }

    private static Result<string, Error> NormalizeTitle(string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            return Errors.Event.TitleRequired();
        if (trimmed.Length > MaxTitleLength)
            return Errors.Event.TitleTooLong(MaxTitleLength);
        return Result.Success<string, Error>(trimmed);
    }

    private static string Serialize(IReadOnlyCollection<int> days) =>
        string.Join(",", days.Where(AllowedLeadDays.Contains).Distinct().OrderBy(d => d));

    private static IReadOnlyList<int> Parse(string csv) =>
        string.IsNullOrEmpty(csv)
            ? Array.Empty<int>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
}
