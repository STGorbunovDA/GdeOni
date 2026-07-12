using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace GdeOni.Mobile.Controls;

/// <summary>Координаты, выбранные тапом по карте.</summary>
public sealed class LocationPickedEventArgs(double latitude, double longitude) : EventArgs
{
    public double Latitude { get; } = latitude;
    public double Longitude { get; } = longitude;
}

/// <summary>
/// E7+/E20+. Карта-пикер координат на WebView + Leaflet/OpenStreetMap —
/// без Google Maps SDK и API-ключей (нативный Map на Android требует
/// google.android.geo.API_KEY, которого у нас нет). Тап по карте отдаёт
/// координаты через событие <see cref="LocationPicked"/>. Внешние
/// координаты (ручной ввод / «Получить координаты») подставляются
/// методом <see cref="SetPoint"/> — маркер двигается, первый раз карта
/// центрируется и зумится на точку.
///
/// Мост JS→C#: по клику страница делает
/// <c>location.href = "gdeoni-pick:&lt;lat&gt;,&lt;lon&gt;"</c>, а
/// <see cref="WebView.Navigating"/> перехватывает и отменяет навигацию.
/// </summary>
public sealed class MapPickerView : ContentView
{
    private const string PickScheme = "gdeoni-pick:";

    private readonly WebView _web;
    private bool _loaded;
    private (double lat, double lon)? _pending;

    public event EventHandler<LocationPickedEventArgs>? LocationPicked;

    public MapPickerView()
    {
        _web = new WebView
        {
            HeightRequest = 280,
            Source = new HtmlWebViewSource { Html = Html },
        };
        _web.Navigating += OnNavigating;
        _web.Navigated += OnNavigated;

        Content = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = _web,
        };
    }

    /// <summary>Ставит/двигает маркер и (в первый раз) центрирует карту.</summary>
    public void SetPoint(double latitude, double longitude)
    {
        if (!_loaded)
        {
            _pending = (latitude, longitude);
            return;
        }

        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        _ = _web.EvaluateJavaScriptAsync($"setPoint({lat},{lon})");
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        _loaded = true;
        if (_pending is { } p)
        {
            _pending = null;
            SetPoint(p.lat, p.lon);
        }
    }

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url is null ||
            !e.Url.StartsWith(PickScheme, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;

        var parts = e.Url.Substring(PickScheme.Length).Split(',');
        if (parts.Length != 2)
            return;

        if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            LocationPicked?.Invoke(this, new LocationPickedEventArgs(lat, lon));
        }
    }

    // Leaflet тянется с CDN (unpkg) — приложению всё равно нужен интернет.
    // Клик по карте отдаёт координаты через custom-scheme навигацию,
    // setPoint(lat,lon) вызывается из C# для синхронизации маркера.
    private const string Html = """
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no">
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<style>html,body,#map{height:100%;margin:0;padding:0}</style>
</head>
<body>
<div id="map"></div>
<script>
  var map = L.map('map').setView([55.751244, 37.618423], 10);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19, attribution: '&copy; OpenStreetMap'
  }).addTo(map);
  // Убираем дефолтный префикс Leaflet (в 1.9 в нём флаг Украины) —
  // оставляем только «© OpenStreetMap» (без флага и страны).
  map.attributionControl.setPrefix(false);

  var marker = null, focused = false;
  var pin = L.divIcon({
    className: '',
    html: '<div style="font-size:30px;line-height:1">📍</div>',
    iconSize: [30, 30], iconAnchor: [9, 30]
  });

  function place(lat, lon) {
    if (marker) { marker.setLatLng([lat, lon]); }
    else { marker = L.marker([lat, lon], { icon: pin }).addTo(map); }
  }

  // Вызывается из C# (EvaluateJavaScriptAsync).
  function setPoint(lat, lon) {
    place(lat, lon);
    if (!focused) { map.setView([lat, lon], 17); focused = true; }
  }

  map.on('click', function (e) {
    place(e.latlng.lat, e.latlng.lng);
    if (!focused) { focused = true; }
    window.location.href = 'gdeoni-pick:' + e.latlng.lat + ',' + e.latlng.lng;
  });
</script>
</body>
</html>
""";
}
