namespace GdeOni.Mobile.Shared.Memories;

/// <summary>
/// Клиентская проверка "может ли текущий юзер редактировать/удалять
/// воспоминание". Backend всё равно отдаст 403 если прав нет — это
/// просто оптимизация UI, чтобы не показывать недоступные кнопки.
///
/// Правила (зеркало backend):
/// - автор воспоминания (memory.AuthorUserId == currentUserId);
/// - автор карточки умершего (cardCreatedByUserId == currentUserId);
/// - админ (mobile это не знает; на стороне сервера у админа всегда true).
/// </summary>
public static class MemoryEditPermission
{
    public static bool CanEdit(
        Guid? currentUserId,
        Guid? memoryAuthorUserId,
        Guid? cardCreatedByUserId) =>
        currentUserId.HasValue
        && (memoryAuthorUserId == currentUserId.Value
            || cardCreatedByUserId == currentUserId.Value);
}
