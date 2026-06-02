using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminUserDetailsPage : ContentPage
{
    public AdminUserDetailsPage(AdminUserDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
