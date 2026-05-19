using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Route;

public partial class RoutePage : ContentPage
{
    private readonly RouteViewModel _viewModel;

    public RoutePage(RouteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Перезагружаем при каждом возврате — юзер мог добавить / удалить
        // карточку через другие экраны, либо изменить координаты могилы.
        await _viewModel.LoadAsync();
    }

    private void OnCandidateTapped(object? sender, TappedEventArgs e)
    {
        // Делаем всю карточку touch-target'ом для чекбокса — на мобильном
        // попадать пальцем в маленький квадрат CheckBox неудобно.
        if (sender is View view && view.BindingContext is RouteCandidateViewModel item)
            item.IsSelected = !item.IsSelected;
    }

    private async void OnBuildRouteClicked(object? sender, EventArgs e)
    {
        // По решению 2026-05-13 на UI оставлен только Яндекс — открываем
        // напрямую, без выбора провайдера. Сервисы Google / 2ГИС в
        // ExternalMapsService.cs не удалены: при необходимости вернуть
        // ActionSheet с тремя кнопками — это локальная правка здесь.
        await _viewModel.BuildRouteCommand.ExecuteAsync("Yandex");
    }
}
