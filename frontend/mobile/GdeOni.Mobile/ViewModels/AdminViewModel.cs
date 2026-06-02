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
}
