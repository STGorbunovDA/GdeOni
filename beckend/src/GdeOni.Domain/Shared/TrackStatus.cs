namespace GdeOni.Domain.Shared;

public enum TrackStatus
{
    /// <summary>
    /// Активное отслеживание
    /// </summary>
    Active = 1,
    /// <summary>
    /// Уведомления уже отключены
    /// </summary>
    Muted = 2,
    /// <summary>
    /// Запись уже в архиве
    /// </summary>
    Archived = 3
}