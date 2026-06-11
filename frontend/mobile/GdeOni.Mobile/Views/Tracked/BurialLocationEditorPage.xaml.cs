using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class BurialLocationEditorPage : ContentPage
{
    public BurialLocationEditorPage(BurialLocationEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
