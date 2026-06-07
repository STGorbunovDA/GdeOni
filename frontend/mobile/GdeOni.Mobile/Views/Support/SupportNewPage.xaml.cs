using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Support;

public partial class SupportNewPage : ContentPage
{
    public SupportNewPage(SupportNewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
