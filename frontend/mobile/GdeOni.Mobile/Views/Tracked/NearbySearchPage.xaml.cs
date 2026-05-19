using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class NearbySearchPage : ContentPage
{
    public NearbySearchPage(NearbySearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
