using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.Shared.EditsHistory;
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

    /// <summary>Сырая лента с бэка (нефильтрованная client-side).</summary>
    private readonly ObservableCollection<AllEditsEntry> _allEdits = new();

    /// <summary>
    /// Видимая коллекция после client-side фильтра по умершему/редактору.
    /// Биндинг XAML — на неё.
    /// </summary>
    public ObservableCollection<AllEditsEntry> Edits { get; } = new();

    /// <summary>
    /// Поиск по ФИО умершего. Применяется client-side к уже загруженной
    /// ленте — не дёргает бэк. Для глубокого поиска можно
    /// "Загрузить ещё" и фильтр применится к новым страницам.
    /// </summary>
    [ObservableProperty] private string _deceasedSearch = "";

    /// <summary>Поиск по email/имени редактора. Тоже client-side.</summary>
    [ObservableProperty] private string _editorSearch = "";

    // Server-side фильтр дат через бэк (точный, не теряем данные).
    [ObservableProperty] private bool _isDateFilterEnabled;
    [ObservableProperty] private DateTime _editedFrom = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _editedTo = DateTime.Today;

    partial void OnDeceasedSearchChanged(string value) => ReapplyClientFilter();
    partial void OnEditorSearchChanged(string value) => ReapplyClientFilter();

    partial void OnIsDateFilterEnabledChanged(bool value)
    {
        if (!HasLoadedOnce) return;
        _ = LoadFirstPageAsync();
    }
    partial void OnEditedFromChanged(DateTime value)
    {
        if (!HasLoadedOnce || !IsDateFilterEnabled) return;
        _ = LoadFirstPageAsync();
    }
    partial void OnEditedToChanged(DateTime value)
    {
        if (!HasLoadedOnce || !IsDateFilterEnabled) return;
        _ = LoadFirstPageAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        DeceasedSearch = "";
        EditorSearch = "";
        IsDateFilterEnabled = false;
        await LoadFirstPageAsync();
    }

    private void ReapplyClientFilter()
    {
        Edits.Clear();
        var deceasedTerm = DeceasedSearch?.Trim();
        var editorTerm = EditorSearch?.Trim();
        foreach (var item in _allEdits)
        {
            var matchesDeceased = string.IsNullOrEmpty(deceasedTerm)
                || item.DeceasedFullName.Contains(deceasedTerm, StringComparison.OrdinalIgnoreCase);
            var matchesEditor = string.IsNullOrEmpty(editorTerm)
                || item.Editor.Contains(editorTerm, StringComparison.OrdinalIgnoreCase);
            if (matchesDeceased && matchesEditor)
                Edits.Add(item);
        }
        OnPropertyChanged(nameof(HasNoItems));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    private bool _hasLoadedOnce;

    public bool HasNoItems => HasLoadedOnce && Edits.Count == 0 && !HasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;

    public bool CanLoadMore => _allEdits.Count < TotalCount && !IsLoading;

    [RelayCommand]
    private async Task LoadFirstPageAsync()
    {
        _allEdits.Clear();
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

            DateTime? fromUtc = IsDateFilterEnabled
                ? DateTime.SpecifyKind(EditedFrom.Date, DateTimeKind.Utc) : null;
            DateTime? toUtc = IsDateFilterEnabled
                ? DateTime.SpecifyKind(EditedTo.Date, DateTimeKind.Utc) : null;

            var envelope = await adminApi.GetAllEditsAsync(
                _nextPage, PageSize, null, null, fromUtc, toUtc);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить ленту правок.";
                return;
            }

            foreach (var item in envelope.Result.Items)
                _allEdits.Add(AllEditsEntry.From(item));

            ReapplyClientFilter();

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
        // Из админ-ленты — на preview, не на details. Details требует чтобы
        // текущий юзер трекал эту карточку (404 от /me/tracked-deceased/{id}
        // иначе), а админ может не отслеживать. Preview работает у всех.
        await Shell.Current.GoToAsync($"deceased-preview?deceasedId={entry.DeceasedId}");
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
            "Reassignment" => "Переуступка (удаление автора)",
            _ => dto.Kind,
        };

        // Reassignment: EditedByUserId всегда NULL (системная операция при
        // удалении юзера), но email удалённого юзера лежит в
        // ChangesJson.PreviousAuthor.Old — показываем его, иначе фразу
        // "Удалённый пользователь" видеть бесполезно (не понятно кто).
        string editor;
        if (dto.Kind == "Reassignment")
        {
            var deletedEmail = EditsHistoryParser.ExtractPreviousAuthorEmail(dto.ChangesJson);
            editor = deletedEmail is not null
                ? $"Удалённый пользователь ({deletedEmail})"
                : "Удалённый пользователь";
        }
        else
        {
            editor = !string.IsNullOrWhiteSpace(dto.EditedByDisplayName)
                ? $"{dto.EditedByDisplayName} ({dto.EditedByEmail})"
                : dto.EditedByEmail ?? "Удалённый пользователь";
        }

        var changes = EditsHistoryParser.ParseChanges(dto.ChangesJson)
            .Select(r => new ChangeRow(r.FieldLabel, r.OldValue, r.NewValue))
            .ToList();

        return new AllEditsEntry
        {
            DeceasedId = dto.DeceasedId,
            DeceasedFullName = dto.DeceasedFullName,
            EditedAt = dto.EditedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            Editor = editor,
            KindDisplay = kindDisplay,
            Changes = changes,
        };
    }
}
