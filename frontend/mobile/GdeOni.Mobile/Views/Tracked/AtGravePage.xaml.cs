using System.ComponentModel;
using GdeOni.Mobile.Controls;
using GdeOni.Mobile.Shared.Utils;
using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class AtGravePage : ContentPage
{
    private readonly AtGraveViewModel _viewModel;
    private bool _initialGeoRequested;

    public AtGravePage(AtGraveViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Тап по карте → координаты в форму; изменение полей координат
        // (геолокация / ручной ввод) → двигаем маркер на карте.
        MapPicker.LocationPicked += OnMapLocationPicked;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Сразу показываем баннер про VPN, если он включён — до того,
        // как юзер вообще запросит координаты.
        _viewModel.RefreshVpnStatus();

        // При первом открытии экрана сразу пробуем получить координаты —
        // юзеру не надо тапать "Получить" каждый раз. Если откажет — он
        // увидит сообщение и сможет повторить вручную.
        if (!_initialGeoRequested)
        {
            _initialGeoRequested = true;
            await _viewModel.RequestLocationCommand.ExecuteAsync(null);
        }
    }

    private void OnMapLocationPicked(object? sender, LocationPickedEventArgs e)
        => _viewModel.ApplyPickedLocation(e.Latitude, e.Longitude);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(AtGraveViewModel.LatitudeInput)
            or nameof(AtGraveViewModel.LongitudeInput)))
        {
            return;
        }

        if (CoordinateParser.TryParseLatitude(_viewModel.LatitudeInput, out var lat)
            && CoordinateParser.TryParseLongitude(_viewModel.LongitudeInput, out var lon))
        {
            MapPicker.SetPoint(lat, lon);
        }
    }
}
