using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminInfoPage : ContentPage
{
    private readonly AdminInfoViewModel _viewModel;
    private bool _initialLoadDone;

    public AdminInfoPage(AdminInfoViewModel viewModel)
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
        await _viewModel.LoadAsync();
    }
}
