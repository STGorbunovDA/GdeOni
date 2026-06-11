using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// F17.9 mobile. Корневой экран админ-вкладки — простое меню разделов.
/// Реальные данные грузятся внутри секций (AllEdits/AdminUsers/AdminPayments).
/// </summary>
public partial class AdminViewModel : ObservableObject
{
    [RelayCommand]
    private async Task OpenAllEditsAsync()
        => await Shell.Current.GoToAsync("all-edits");

    [RelayCommand]
    private async Task OpenAdminUsersAsync()
        => await Shell.Current.GoToAsync("admin-users");

    [RelayCommand]
    private async Task OpenAdminPaymentsAsync()
        => await Shell.Current.GoToAsync("admin-payments");

    /// <summary>D25. Обращения в службу поддержки + автоматические инциденты.</summary>
    [RelayCommand]
    private async Task OpenAdminSupportAsync()
        => await Shell.Current.GoToAsync("admin-support");

    /// <summary>
    /// D27. Поиск умершего по всем характеристикам с полным admin-просмотром
    /// карточки (медиа, координаты, верификация) без добавления её в
    /// отслеживание.
    /// </summary>
    [RelayCommand]
    private async Task OpenAdminFindDeceasedAsync()
        => await Shell.Current.GoToAsync("admin-find-deceased");

    /// <summary>
    /// Возврат на профиль (откуда обычно попадают). ".." делает pop,
    /// если переход был через push; если AdminPage оказалась корневой
    /// в стеке — открываем профиль абсолютным путём как fallback.
    /// </summary>
    [RelayCommand]
    private async Task BackAsync()
    {
        try { await Shell.Current.GoToAsync(".."); }
        catch { await Shell.Current.GoToAsync("//main/profile"); }
    }
}
