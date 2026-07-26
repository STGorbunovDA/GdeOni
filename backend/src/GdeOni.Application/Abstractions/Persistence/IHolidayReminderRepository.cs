using GdeOni.Domain.Aggregates.Events;

namespace GdeOni.Application.Abstractions.Persistence;

/// <summary>
/// Хранилище персональных настроек напоминаний о праздниках. Хранятся только
/// явные пользовательские настройки (по ключу праздника); дефолты для
/// «крупных»/«мелких» считает клиент.
/// </summary>
public interface IHolidayReminderRepository
{
    /// <summary>Все настройки напоминаний пользователя.</summary>
    Task<IReadOnlyList<HolidayReminder>> GetByUser(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>Настройка по конкретному празднику (или null).</summary>
    Task<HolidayReminder?> GetByUserAndKey(
        Guid userId,
        string holidayKey,
        CancellationToken cancellationToken);

    Task Add(HolidayReminder reminder, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}
