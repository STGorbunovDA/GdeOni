using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Events;

public partial class EventsPage : ContentPage
{
    private readonly EventsViewModel _viewModel;

    public EventsPage(EventsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Перезагружаем при каждом возврате — годовщины/праздники
        // зависят от текущей даты, а список отслеживаемых мог измениться.
        await _viewModel.LoadAsync();
    }
}
