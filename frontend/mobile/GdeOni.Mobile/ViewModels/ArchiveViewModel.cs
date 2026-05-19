using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

public partial class ArchiveViewModel(ITrackedDeceasedApi api) : ObservableObject
{
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
    public bool ShowEmptyState => HasLoadedOnce && !IsBusy && Items.Count == 0;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var envelope = await api.GetListAsync(page: 1, pageSize: 100);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить архив.";
                return;
            }

            Items.Clear();
            foreach (var item in envelope.Result.Items)
            {
                if (item.Status != TrackStatuses.Archived)
                    continue;
                Items.Add(item);
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
        try { await LoadAsync(); }
        finally { IsRefreshing = false; }
    }

    [RelayCommand]
    private async Task OpenItemAsync(TrackedDeceasedListItem? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"deceased-details?deceasedId={item.DeceasedId}");
    }

    /// <summary>
    /// Удалить tracking-запись навсегда (DELETE /api/users/me/tracked-deceased/{id}).
    /// Сама карточка умершего не трогается — её удаляет только админ
    /// или автор через отдельный endpoint (см. F8/E13). Это финальная
    /// ступень "слоистого" удаления: сначала юзер архивирует, потом
    /// в архиве может убрать совсем.
    /// </summary>
    [RelayCommand]
    private async Task UntrackForeverAsync(TrackedDeceasedListItem? item)
    {
        if (item is null) return;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var confirmed = await page.DisplayAlertAsync(
            "Удалить из отслеживания навсегда?",
            $"{item.FullName} полностью пропадёт из вашего списка. Сама карточка умершего останется в системе — её сможет вернуть только админ.",
            "Удалить навсегда",
            "Отмена");
        if (!confirmed) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var resp = await api.UntrackAsync(item.DeceasedId);
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Не удалось удалить (HTTP {(int)resp.StatusCode}).";
                return;
            }

            Items.Remove(item);
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
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("//main/tracked");
    }
}
