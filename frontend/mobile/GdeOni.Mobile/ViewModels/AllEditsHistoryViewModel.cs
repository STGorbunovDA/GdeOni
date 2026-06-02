using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// F17.9 mobile. Глобальная лента правок (с указанием карточки умершего)
/// для админ-вкладки. По тапу на запись открывается карточка.
/// </summary>
public partial class AllEditsHistoryViewModel(IAdminApi adminApi) : ObservableObject
{
    private const int PageSize = 20;
    private int _nextPage = 1;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<AllEditsEntry> Edits { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    private bool _hasLoadedOnce;

    public bool HasNoItems => HasLoadedOnce && Edits.Count == 0 && !HasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;

    public bool CanLoadMore => Edits.Count < TotalCount && !IsLoading;

    [RelayCommand]
    private async Task LoadFirstPageAsync()
    {
        Edits.Clear();
        _nextPage = 1;
        TotalCount = 0;
        HasLoadedOnce = false;
        await LoadNextPageAsync();
    }

    [RelayCommand]
    private async Task LoadNextPageAsync()
    {
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var envelope = await adminApi.GetAllEditsAsync(_nextPage, PageSize);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить ленту правок.";
                return;
            }

            foreach (var item in envelope.Result.Items)
                Edits.Add(AllEditsEntry.From(item));

            TotalCount = envelope.Result.TotalCount;
            _nextPage++;
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = (int)apiEx.StatusCode switch
            {
                403 => "Доступ только для админов.",
                _ => $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            HasLoadedOnce = true;
            OnPropertyChanged(nameof(HasNoItems));
            OnPropertyChanged(nameof(CanLoadMore));
        }
    }

    [RelayCommand]
    private async Task OpenDeceasedAsync(AllEditsEntry? entry)
    {
        if (entry is null) return;
        await Shell.Current.GoToAsync($"deceased-details?deceasedId={entry.DeceasedId}");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

public sealed class AllEditsEntry
{
    public Guid DeceasedId { get; init; }
    public string DeceasedFullName { get; init; } = "";
    public string EditedAt { get; init; } = "";
    public string Editor { get; init; } = "";
    public string KindDisplay { get; init; } = "";
    public IReadOnlyList<ChangeRow> Changes { get; init; } = Array.Empty<ChangeRow>();

    public static AllEditsEntry From(AllEditsItem dto)
    {
        var kindDisplay = dto.Kind switch
        {
            "MainInfo" => "Основное",
            "Metadata" => "Дополнительно",
            "BurialLocation" => "Место захоронения",
            _ => dto.Kind,
        };

        var editor = !string.IsNullOrWhiteSpace(dto.EditedByDisplayName)
            ? $"{dto.EditedByDisplayName} ({dto.EditedByEmail})"
            : dto.EditedByEmail ?? "Удалённый пользователь";

        return new AllEditsEntry
        {
            DeceasedId = dto.DeceasedId,
            DeceasedFullName = dto.DeceasedFullName,
            EditedAt = dto.EditedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            Editor = editor,
            KindDisplay = kindDisplay,
            Changes = ParseChanges(dto.ChangesJson),
        };
    }

    private static IReadOnlyList<ChangeRow> ParseChanges(string json)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, ChangePairDto>>(json);
            if (dict is null) return Array.Empty<ChangeRow>();
            return dict.Select(kv => new ChangeRow(
                FieldDisplay(kv.Key),
                kv.Value.Old ?? "—",
                kv.Value.New ?? "—")).ToList();
        }
        catch
        {
            return new[] { new ChangeRow("(diff)", "", json) };
        }
    }

    private static string FieldDisplay(string field) => field switch
    {
        "FirstName" => "Имя",
        "LastName" => "Фамилия",
        "MiddleName" => "Отчество",
        "BirthDate" => "Дата рождения",
        "DeathDate" => "Дата смерти",
        "ShortDescription" => "Краткое описание",
        "Biography" => "Биография",
        "Epitaph" => "Эпитафия",
        "Religion" => "Религия",
        "Source" => "Источник",
        "IsMilitaryService" => "Военная служба",
        "AdditionalInfo" => "Доп. информация",
        "Latitude" => "Широта",
        "Longitude" => "Долгота",
        "AccuracyMeters" => "Точность, м",
        "Country" => "Страна",
        "City" => "Город",
        "CemeteryName" => "Кладбище",
        "PlotNumber" => "Участок",
        "GraveNumber" => "Номер могилы",
        _ => field,
    };

    private sealed record ChangePairDto(string? Old, string? New);
}
