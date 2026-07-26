using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Services.Media;
using GdeOni.Mobile.Services.Versioning;
using Refit;

namespace GdeOni.Mobile.ViewModels;

public partial class TrackedListViewModel(
    ITrackedDeceasedApi api,
    IPublicHostsService publicHosts,
    IAppUpdateState appUpdateState,
    IEventsApi eventsApi) : ObservableObject
{
    // F42. Попап «сегодня праздник» показываем один раз в день.
    private const string HolidayPopupShownKey = "gdeoni-holiday-popup-shown";

    [ObservableProperty]
    private string _title = "Поиск";

    // E22. Мягкое обновление «доступна новая версия» показываем модальным
    // диалогом поверх экрана при заходе на главную (см. MaybePromptUpdateAsync).
    // Факт кладёт AppShell после проверки версии в singleton IAppUpdateState.

    /// <summary>
    /// Если доступно мягкое обновление — один раз за запуск показывает поверх
    /// экрана диалог «Доступна новая версия» с выбором «Обновить»/«Позже».
    /// «Обновить» открывает страницу скачивания APK во внешнем браузере.
    /// Зовётся из OnAppearing главной страницы.
    /// </summary>
    public async Task MaybePromptUpdateAsync()
    {
        if (!appUpdateState.IsSoftUpdateAvailable)
            return;

        // Показываем один раз за запуск приложения — чтобы не всплывало при
        // каждом возврате на вкладку.
        appUpdateState.MarkPrompted();

        var update = await Shell.Current.DisplayAlert(
            "Доступна новая версия",
            "Вышла новая версия приложения. Обновить сейчас?",
            "Обновить",
            "Позже");

        if (!update)
            return;

        var url = appUpdateState.DownloadUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            await Launcher.Default.OpenAsync(new Uri(url));
        }
        catch
        {
            // Невалидный URL — показывать ошибку негде; юзер увидит, что
            // браузер не открылся, и попробует снова.
        }
    }

    /// <summary>
    /// F42. Если сегодня крупный праздник — один раз в день показать попап
    /// «Сегодня праздник: …». Зовётся из OnAppearing главной (при заходе).
    /// Best-effort: ошибки/сеть глушим. Эндпоинт праздников не за paywall'ом.
    /// </summary>
    public async Task MaybeShowHolidayPopupAsync()
    {
        try
        {
            var todayIso = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            // Проверяем ДО запроса — если уже показывали сегодня, сеть не дёргаем.
            if (Preferences.Get(HolidayPopupShownKey, string.Empty) == todayIso)
                return;

            var envelope = await eventsApi.GetHolidaysAsync(todayIso, todayIso);
            var major = (envelope.Result?.Holidays ?? Array.Empty<HolidayDto>())
                .Where(h => h.IsMajor)
                .Select(h => h.Name)
                .ToList();
            if (major.Count == 0)
                return;

            // Помечаем показанным до самого показа — чтобы не всплыл повторно.
            Preferences.Set(HolidayPopupShownKey, todayIso);

            var body = string.Join(Environment.NewLine, major.Select(n => "• " + n));
            await Shell.Current.DisplayAlert("Сегодня праздник", body, "ОК");
        }
        catch
        {
            // Попап не критичен — молча пропускаем при любой ошибке.
        }
    }

    public ObservableCollection<TrackedDeceasedListItem> Items { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _hasLoadedOnce;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Empty-state карточка показывается только после первой завершённой
    /// загрузки и только если список реально пуст. Иначе при холодном
    /// старте на секунду мелькало бы "никого не отслеживаете".
    /// </summary>
    public bool ShowEmptyState => HasLoadedOnce && !IsBusy && Items.Count == 0;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var envelope = await api.GetListAsync(page: 1, pageSize: 50);

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить список.";
                return;
            }

            Items.Clear();
            // Архивные карточки на главном экране не показываем — для них
            // отдельный экран "Архив". Muted остаётся (статус уведомлений,
            // не статус отслеживания).
            foreach (var item in envelope.Result.Items)
            {
                if (item.Status == TrackStatuses.Archived)
                    continue;
                // D36: подменяем MainPhotoUrl на собранный из bucket+key
                // через PublicHostsService. Старый deprecated MainPhotoUrl
                // от бэка (если есть) перезаписывается; XAML биндится на
                // ту же property, ничего не знает о подмене.
                var resolved = await item.ResolveMainPhotoAsync(publicHosts);
                Items.Add(resolved);
            }

            OnPropertyChanged(nameof(ShowEmptyState));
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
            HasLoadedOnce = true;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// "+ Добавить умершего" → сначала экран поиска (E16). Если юзер
    /// найдёт уже существующую карточку, он подпишется в один тап;
    /// если не найдёт — кнопка "Создать новую" в DeceasedSearchPage
    /// перейдёт на AtGravePage.
    /// </summary>
    [RelayCommand]
    private async Task AddDeceasedAsync()
    {
        await Shell.Current.GoToAsync("deceased-search");
    }

    [RelayCommand]
    private async Task OpenArchiveAsync()
    {
        await Shell.Current.GoToAsync("archive");
    }

    /// <summary>
    /// E21. Открыть страницу "Найти рядом" — геолокация и поиск умерших
    /// в радиусе. Сценарий "стою на кладбище, хочу понять кто рядом".
    /// </summary>
    [RelayCommand]
    private async Task OpenNearbySearchAsync()
    {
        await Shell.Current.GoToAsync("nearby-search");
    }

    [RelayCommand]
    private async Task OpenItemAsync(TrackedDeceasedListItem? item)
    {
        if (item is null)
            return;

        await Shell.Current.GoToAsync($"deceased-details?deceasedId={item.DeceasedId}");
    }
}
