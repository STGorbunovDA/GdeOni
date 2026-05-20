using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Profile;

public partial class SubscriptionPage : ContentPage
{
    private readonly SubscriptionViewModel _viewModel;

    public SubscriptionPage(SubscriptionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
