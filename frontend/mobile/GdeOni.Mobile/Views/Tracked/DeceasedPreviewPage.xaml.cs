using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class DeceasedPreviewPage : ContentPage
{
    public DeceasedPreviewPage(DeceasedPreviewViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
