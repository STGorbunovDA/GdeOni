using System.ComponentModel;
using GdeOni.Mobile.Controls;
using GdeOni.Mobile.Shared.Utils;
using GdeOni.Mobile.ViewModels;

namespace GdeOni.Mobile.Views.Tracked;

public partial class BurialLocationEditorPage : ContentPage
{
    private readonly BurialLocationEditorViewModel _viewModel;

    public BurialLocationEditorPage(BurialLocationEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Тап по карте → координаты в форму; изменение полей координат
        // (геолокация / ручной ввод / стартовые из карточки) → двигаем маркер.
        MapPicker.LocationPicked += OnMapLocationPicked;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnMapLocationPicked(object? sender, LocationPickedEventArgs e)
        => _viewModel.ApplyPickedLocation(e.Latitude, e.Longitude);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(BurialLocationEditorViewModel.LatitudeInput)
            or nameof(BurialLocationEditorViewModel.LongitudeInput)))
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
