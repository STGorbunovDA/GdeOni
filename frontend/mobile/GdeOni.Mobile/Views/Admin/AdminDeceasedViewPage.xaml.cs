using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminDeceasedViewPage : ContentPage
{
    public AdminDeceasedViewPage(AdminDeceasedViewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
