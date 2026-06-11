using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class MemoryEditorPage : ContentPage
{
    public MemoryEditorPage(MemoryEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
