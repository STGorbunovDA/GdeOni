using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class TrackedListPage : ContentPage
{
    private readonly TrackedListViewModel _viewModel;

    public TrackedListPage(TrackedListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // E22. Перечитать состояние мягкого баннера обновления (могло
        // выставиться в AppShell после создания этой VM).
        _viewModel.RefreshUpdateBanner();
        // Подгружаем список при возврате на вкладку — после создания
        // карточки в at-grave новый item должен появиться сразу.
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
