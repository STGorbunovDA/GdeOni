using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Support;

public partial class SupportDetailsPage : ContentPage
{
    public SupportDetailsPage(SupportDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
