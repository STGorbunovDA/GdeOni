using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminSupportPage : ContentPage
{
    private readonly AdminSupportViewModel _viewModel;
    private bool _initialLoadDone;

    public AdminSupportPage(AdminSupportViewModel viewModel)
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
