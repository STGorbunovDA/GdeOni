namespace GdeOni.Infrastructure.Relatives;

/// <summary>
/// Функция «Родственники» (Фаза 4). Запись «этому владельцу уже сообщили про
/// такого-то родственника по такой-то карточке». Не доменный агрегат, а
/// чисто инфраструктурный лог — как <c>SentAnniversaryEmail</c>: ночной джоб
/// <see cref="RelativeDiscoveryService"/> раз в сутки находит новые пары
/// «(владелец, умерший, родственник)» и заводит по каждой строку с
/// <see cref="IsNew"/> = true. Пока флаг не сброшен — на входе в приложение
/// в попапе «События» показывается «у вас новый родственник».
///
/// Уникальность обеспечена индексом
/// <see cref="GdeOni.Domain.Shared.DbConstraints.UxRelativeDiscoveries"/>:
/// одна запись на (владелец, умерший, родственник), поэтому повторный прогон
/// джоба не задублирует уведомление.
/// </summary>
internal sealed class RelativeDiscovery
{
    public Guid Id { get; private set; }

    /// <summary>Пользователь, которому адресовано уведомление.</summary>
    public Guid OwnerUserId { get; private set; }

    public Guid DeceasedId { get; private set; }

    /// <summary>Найденный «родственник» — другой отслеживающий карточку.</summary>
    public Guid RelativeUserId { get; private set; }

    public DateTime DiscoveredAtUtc { get; private set; }

    /// <summary>
    /// true — уведомление ещё «новое» (владелец не заходил на вкладку
    /// «Родственники» после обнаружения). Сбрасывается в false при заходе.
    /// </summary>
    public bool IsNew { get; private set; }

    private RelativeDiscovery() { }

    public static RelativeDiscovery Create(
        Guid ownerUserId,
        Guid deceasedId,
        Guid relativeUserId,
        DateTime discoveredAtUtc) => new()
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            DeceasedId = deceasedId,
            RelativeUserId = relativeUserId,
            DiscoveredAtUtc = discoveredAtUtc,
            IsNew = true,
        };
}
