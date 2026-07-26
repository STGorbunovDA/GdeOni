using CSharpFunctionalExtensions;

namespace GdeOni.Domain.Aggregates.Events;

/// <summary>
/// Персональная настройка напоминания о празднике. Привязка к празднику —
/// по стабильному ключу (имени праздника): даты движутся год к году, а сам
/// праздник один, поэтому напоминание работает каждый год.
///
/// <see cref="LeadDaysCsv"/> — набор «за сколько дней напомнить», сериализован
/// в CSV («0,1,3,7»): 0 = в день, 1 = за день, 3 = за 3 дня, 7 = за неделю.
/// Пустая строка = напоминание отключено. Хранение строкой намеренно
/// провайдеро-независимое (обычная колонка, без массивов Postgres). Дефолты
/// (для крупных — «в день», для мелких — выключено) считает клиент по флагу
/// Holiday.IsMajor; здесь лежат только явные пользовательские настройки.
/// </summary>
public sealed class HolidayReminder : Entity<Guid>
{
    public const int MaxHolidayKeyLength = 200;

    public Guid UserId { get; private set; }
    public string HolidayKey { get; private set; }
    public string LeadDaysCsv { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Разобранный набор «за сколько дней» (для чтения клиентом).</summary>
    public IReadOnlyList<int> LeadDays => Parse(LeadDaysCsv);

    private HolidayReminder() : base(Guid.Empty)
    {
        HolidayKey = null!;
        LeadDaysCsv = string.Empty;
    }

    private HolidayReminder(
        Guid id,
        Guid userId,
        string holidayKey,
        string leadDaysCsv,
        DateTime nowUtc) : base(id)
    {
        UserId = userId;
        HolidayKey = holidayKey;
        LeadDaysCsv = leadDaysCsv;
        UpdatedAtUtc = nowUtc;
    }

    public static HolidayReminder Create(
        Guid userId,
        string holidayKey,
        IReadOnlyCollection<int> leadDays,
        DateTime nowUtc) =>
        new(Guid.NewGuid(), userId, holidayKey.Trim(), Serialize(leadDays), nowUtc);

    /// <summary>
    /// Обновить набор «за сколько дней». No-op при структурно тех же значениях
    /// (не трогаем UpdatedAtUtc). Пустой набор = отключить напоминание.
    /// </summary>
    public void SetLeadDays(IReadOnlyCollection<int> leadDays, DateTime nowUtc)
    {
        var csv = Serialize(leadDays);
        if (csv == LeadDaysCsv)
            return;

        LeadDaysCsv = csv;
        UpdatedAtUtc = nowUtc;
    }

    private static string Serialize(IReadOnlyCollection<int> days) =>
        string.Join(",", days.Distinct().OrderBy(d => d));

    private static IReadOnlyList<int> Parse(string csv) =>
        string.IsNullOrEmpty(csv)
            ? Array.Empty<int>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
}
