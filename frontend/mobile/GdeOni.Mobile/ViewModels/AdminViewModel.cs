using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Auth;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// F17.9 mobile. Корневой экран админ-вкладки — простое меню разделов.
/// Реальные данные грузятся внутри секций (AllEdits/AdminUsers/AdminPayments).
/// </summary>
public partial class AdminViewModel(IAuthService authService) : ObservableObject
{
    /// <summary>
    /// D44. Раздел обращений доступен только владельцу сервиса: в
    /// переписке платёжные реквизиты и договорённости о переводах.
    /// Стартовое значение false — пункт меню лучше показать с
    /// задержкой, чем мигнуть и исчезнуть.
    ///
    /// Это только UI: бэк закрыт [Authorize(Roles = "SuperAdmin")]
    /// плюс IsSuperAdmin() в use case'ах.
    /// </summary>
    [ObservableProperty]
    private bool _isSuperAdmin;

    /// <summary>
    /// Версия установленного приложения (ApplicationDisplayVersion из csproj).
    /// Показываем в админке, чтобы быстро свериться, что реально стоит на
    /// телефоне — без захода в системные настройки Android.
    /// </summary>
    public string AppVersionDisplay => $"Версия приложения {AppInfo.Current.VersionString}";

    /// <summary>
    /// Подтягивает роль. Ошибку глушим: не смогли определить — раздел
    /// просто не показываем, бэк всё равно не пустит.
    /// </summary>
    public async Task LoadRoleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var me = await authService.GetCurrentUserAsync(cancellationToken);
            IsSuperAdmin = string.Equals(
                me?.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            IsSuperAdmin = false;
        }
    }

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
    /// F38. Справка-сводка по системе: числа по людям, карточкам, контенту,
    /// обращениям и деньгам. Только чтение — зеркало веб-раздела «Информация».
    /// </summary>
    [RelayCommand]
    private async Task OpenAdminInfoAsync()
        => await Shell.Current.GoToAsync("admin-info");

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
