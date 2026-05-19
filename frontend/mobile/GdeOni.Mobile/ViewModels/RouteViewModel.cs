using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Geolocation;
using GdeOni.Mobile.Services.Routing;
using GdeOni.Mobile.Shared.Utils;
using Refit;
using SharedRoutePoint = GdeOni.Mobile.Shared.Routing.RoutePoint;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// E19. Построение маршрута по нескольким могилам. Загружаем
/// tracked-карточки с координатами, юзер выбирает чекбоксами, нажимает
/// "Построить маршрут" → выбирает провайдер (Яндекс/Google/2ГИС) и
/// уходит во внешнее приложение с уже оптимизированным порядком точек.
/// </summary>
public partial class RouteViewModel(
    ITrackedDeceasedApi api,
    IGeolocationService geolocationService,
    IExternalMapsService externalMapsService) : ObservableObject
{
    [ObservableProperty]
    private string _title = "Маршрут";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<RouteCandidateViewModel> Candidates { get; } = new();
    public bool HasCandidates => Candidates.Count > 0;
    public bool HasNoCandidates => Candidates.Count == 0;
    public int SelectedCount => Candidates.Count(c => c.IsSelected);
    public bool HasSelection => SelectedCount > 0;
    public string BuildButtonText => SelectedCount == 0
        ? "Выберите хотя бы одного умершего"
        : $"Построить маршрут ({SelectedCount})";

    public string EmptyText =>
        "У отслеживаемых нет указанных координат места захоронения. Откройте карточку и добавьте координаты, чтобы они появились здесь.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var envelope = await api.GetListAsync(page: 1, pageSize: 100);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить список.";
                return;
            }

            // Сохраняем уже отмеченных, чтобы при перезагрузке списка не
            // сбрасывался выбор (юзер может зайти в карточку и вернуться).
            var previouslySelected = Candidates
                .Where(c => c.IsSelected)
                .Select(c => c.DeceasedId)
                .ToHashSet();

            foreach (var c in Candidates)
                c.PropertyChanged -= OnCandidateChanged;
            Candidates.Clear();

            foreach (var item in envelope.Result.Items)
            {
                // Архивных в маршрут не пускаем — это "удалённые" с точки
                // зрения юзера, см. TrackedListViewModel/ArchiveViewModel:
                // там тот же критерий разделения по Status.
                if (item.Status == TrackStatuses.Archived)
                    continue;

                if (!item.HasGraveLocation
                    || item.GraveLatitude is not double lat
                    || item.GraveLongitude is not double lon)
                {
                    continue;
                }
                var vm = new RouteCandidateViewModel(
                    item.DeceasedId,
                    item.FullName,
                    BuildSubtitle(item),
                    lat,
                    lon)
                {
                    IsSelected = previouslySelected.Contains(item.DeceasedId)
                };
                vm.PropertyChanged += OnCandidateChanged;
                Candidates.Add(vm);
            }

            OnPropertyChanged(nameof(HasCandidates));
            OnPropertyChanged(nameof(HasNoCandidates));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(BuildButtonText));
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = $"Сетевая ошибка: {httpEx.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task BuildRouteAsync(string? providerKey)
    {
        if (!Enum.TryParse<ExternalMapsProvider>(providerKey, ignoreCase: true, out var provider))
            return;

        var selected = Candidates.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Сначала отметьте хотя бы одну могилу.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            // Origin — текущая геопозиция, если разрешена. Если юзер
            // отказал — строим маршрут без origin'а (старт = первая точка
            // после TSP-оптимизации).
            RoutePoint? origin = null;
            var geo = await geolocationService.RequestAndGetCurrentAsync();
            if (geo.Success && geo.Latitude is double oLat && geo.Longitude is double oLon)
            {
                origin = new RoutePoint(oLat, oLon);
            }

            var ordered = OptimizeOrder(origin, selected);
            var routePoints = ordered
                .Select(c => new RoutePoint(c.Latitude, c.Longitude))
                .ToList();

            var ok = await externalMapsService.OpenRouteAsync(provider, origin, routePoints);
            if (!ok)
            {
                ErrorMessage = "Не удалось открыть приложение карт. Возможно, оно не установлено.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var c in Candidates)
            c.IsSelected = false;
    }

    private void OnCandidateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RouteCandidateViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(BuildButtonText));
        }
    }

    // Nearest-neighbor TSP — делегируем в Shared.Utils.RouteOptimizer
    // (юнит-тестируется без MAUI runtime). Здесь только перекладываем
    // RouteCandidateViewModel ↔ Shared.Routing.RoutePoint.
    private static List<RouteCandidateViewModel> OptimizeOrder(
        RoutePoint? origin,
        List<RouteCandidateViewModel> points)
    {
        if (points.Count <= 1) return points;

        var sharedOrigin = origin is null ? null : new SharedRoutePoint(origin.Latitude, origin.Longitude);
        var sharedPoints = points
            .Select(p => new SharedRoutePoint(p.Latitude, p.Longitude))
            .ToList();

        var optimized = RouteOptimizer.OptimizeOrder(sharedOrigin, sharedPoints);

        // Восстанавливаем оригинальные RouteCandidateViewModel по индексу
        // координат — у нас нет идентификаторов в SharedRoutePoint, но мы
        // знаем, что входной порядок sharedPoints[i] = points[i].
        // Мапим обратно через совпадение Lat/Lon (точное — мы их не
        // меняли). При полном совпадении нескольких точек в одном месте
        // (что в реальности почти невозможно для разных могил) берём
        // первую неиспользованную.
        var used = new HashSet<int>();
        var result = new List<RouteCandidateViewModel>(points.Count);
        foreach (var p in optimized)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (used.Contains(i)) continue;
                if (points[i].Latitude == p.Latitude && points[i].Longitude == p.Longitude)
                {
                    used.Add(i);
                    result.Add(points[i]);
                    break;
                }
            }
        }
        return result;
    }

    private static string BuildSubtitle(TrackedDeceasedListItem item)
    {
        var death = item.DeathDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        return $"† {death}";
    }
}

public partial class RouteCandidateViewModel : ObservableObject
{
    public Guid DeceasedId { get; }
    public string FullName { get; }
    public string Subtitle { get; }
    public double Latitude { get; }
    public double Longitude { get; }

    [ObservableProperty]
    private bool _isSelected;

    public RouteCandidateViewModel(
        Guid deceasedId,
        string fullName,
        string subtitle,
        double latitude,
        double longitude)
    {
        DeceasedId = deceasedId;
        FullName = fullName;
        Subtitle = subtitle;
        Latitude = latitude;
        Longitude = longitude;
    }
}
