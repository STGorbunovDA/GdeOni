using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Profile;

public partial class ChangePasswordPage : ContentPage
{
    public ChangePasswordPage(ChangePasswordViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
