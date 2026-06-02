using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminUsersPage : ContentPage
{
    private readonly AdminUsersViewModel _viewModel;
    private bool _initialLoadDone;

    public AdminUsersPage(AdminUsersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_initialLoadDone) return;
        _initialLoadDone = true;
        await _viewModel.LoadFirstPageAsync();
    }
}
