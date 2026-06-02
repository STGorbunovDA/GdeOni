using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AllEditsHistoryPage : ContentPage
{
    private readonly AllEditsHistoryViewModel _viewModel;
    private bool _initialLoadDone;

    public AllEditsHistoryPage(AllEditsHistoryViewModel viewModel)
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
        await _viewModel.LoadFirstPageCommand.ExecuteAsync(null);
    }
}
