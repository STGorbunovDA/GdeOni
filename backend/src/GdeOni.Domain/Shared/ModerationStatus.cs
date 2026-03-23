namespace GdeOni.Domain.Shared;

public enum ModerationStatus
{
    /// <summary>
    /// Ожидаение
    /// </summary>
    Pending = 1,
    /// <summary>
    /// Одобрено
    /// </summary>
    Approved = 2,
    /// <summary>
    /// Отклонено
    /// </summary>
    Rejected = 3
}