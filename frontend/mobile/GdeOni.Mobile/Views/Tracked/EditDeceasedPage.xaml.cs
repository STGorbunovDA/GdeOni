using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class EditDeceasedPage : ContentPage
{
    public EditDeceasedPage(EditDeceasedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
