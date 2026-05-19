using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class DeceasedSearchPage : ContentPage
{
    public DeceasedSearchPage(DeceasedSearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
