using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminUserTrackedPage : ContentPage
{
    public AdminUserTrackedPage(AdminUserTrackedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
