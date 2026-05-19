using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class DeceasedDetailsPage : ContentPage
{
    private readonly DeceasedDetailsViewModel _viewModel;

    public DeceasedDetailsPage(DeceasedDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // После возврата из MemoryEditorPage (POST/PUT/DELETE) перечитываем
        // карточку, чтобы Memories отразили серверное состояние. QueryProperty
        // выставляется только при первом push'е — без OnAppearing-trigger'а
        // обновлений не было бы.
        if (Guid.TryParse(_viewModel.DeceasedId, out _))
            _ = _viewModel.LoadAsync();
    }
}
