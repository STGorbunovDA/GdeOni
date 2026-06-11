using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminUsersPage : ContentPage
{
    private readonly AdminUsersViewModel _viewModel;

    public AdminUsersPage(AdminUsersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Перезагружаем при каждом возврате на страницу — иначе после
    /// смены роли в AdminUserDetailsPage список останется с устаревшими
    /// данными.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFirstPageAsync();
    }
}
