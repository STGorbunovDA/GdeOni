namespace GdeOni.Mobile.Shared.Notifications;

/// <summary>
/// E23. Тип годовщины для локального уведомления. Один tracked-deceased
/// может иметь оба напоминания включёнными независимо.
/// </summary>
public enum AnniversaryKind
{
    Birth = 0,
    Death = 1,
}
