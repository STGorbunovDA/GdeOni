using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Shared.Memories;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// View-model для одного воспоминания в карточке умершего. CanEdit
/// проставлен при загрузке — XAML-биндинг остаётся простым и не зависит
/// от current user / card author state.
///
/// D14: модерация воспоминаний отключена на backend, поэтому badge-поля
/// (StatusBadge / ShowStatusBadge) убраны. Инфраструктура enum-статусов
/// в backend оставлена для возможного будущего антиспама, но в обычном
/// flow воспоминания сразу Approved.
/// </summary>
public sealed record MemoryItemViewModel(
    Guid Id,
    string Text,
    DateTime CreatedAtUtc,
    bool CanEdit,
    string AuthorDisplay)
{
    public static MemoryItemViewModel From(
        DeceasedMemoryResponse memory,
        Guid? cardCreatedByUserId,
        Guid? currentUserId)
    {
        // Делегируем в Shared (юнит-тестируется): автор воспоминания
        // ИЛИ автор карточки. Админу — backend отдаст 200 даже если
        // у mobile CanEdit=false, поэтому здесь спокойно false по умолчанию.
        var canEdit = MemoryEditPermission.CanEdit(
            currentUserId,
            memoryAuthorUserId: memory.AuthorUserId,
            cardCreatedByUserId: cardCreatedByUserId);

        // F12: AuthorName из бэка (FullName ?? UserName); fallback "Аноним"
        // если бэк ещё старый или автор удалил аккаунт.
        var authorDisplay = string.IsNullOrWhiteSpace(memory.AuthorName)
            ? "Аноним"
            : memory.AuthorName;

        return new MemoryItemViewModel(
            memory.Id,
            memory.Text,
            memory.CreatedAtUtc,
            canEdit,
            authorDisplay);
    }
}
