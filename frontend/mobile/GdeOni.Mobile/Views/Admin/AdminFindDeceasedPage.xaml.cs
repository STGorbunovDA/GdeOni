using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Admin;

public partial class AdminFindDeceasedPage : ContentPage
{
    public AdminFindDeceasedPage(AdminFindDeceasedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AdminFindDeceasedViewModel vm)
            await vm.LoadFirstPageAsync();
    }
}
