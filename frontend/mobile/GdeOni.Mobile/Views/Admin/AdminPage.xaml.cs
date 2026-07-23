using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminPage : ContentPage
{
    private readonly AdminViewModel _viewModel;

    public AdminPage(AdminViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// D44. Роль подтягиваем при появлении экрана: раздел обращений
    /// виден только SuperAdmin. В конструкторе делать нельзя — это
    /// сетевой вызов, он не должен блокировать создание страницы.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadRoleAsync();
    }
}
