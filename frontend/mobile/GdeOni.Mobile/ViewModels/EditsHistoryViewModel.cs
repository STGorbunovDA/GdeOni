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
/// F17.9 (mobile-вариант). Админская страница "История правок карточки".
/// Дёргает GET /api/deceased-records/{id}/edits, рендерит дифф из jsonb.
/// Если юзер не админ — бэк вернёт 403; обрабатываем через
/// <see cref="ErrorMessage"/>.
/// </summary>
[QueryProperty(nameof(DeceasedId), "deceasedId")]
public partial class EditsHistoryViewModel(IDeceasedRecordsApi deceasedRecordsApi) : ObservableObject
{
    private const int PageSize = 20;
    private int _nextPage = 1;

    [ObservableProperty] private string _deceasedId = "";

    partial void OnDeceasedIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = LoadFirstPageAsync();
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<EditEntry> Edits { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    private bool _hasLoadedOnce;

    public bool HasNoItems => HasLoadedOnce && Edits.Count == 0 && !HasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    private int _totalCount;

    public bool CanLoadMore => Edits.Count < TotalCount && !IsLoading;

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
        if (!Guid.TryParse(DeceasedId, out var id)) return;
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var envelope = await deceasedRecordsApi.GetEditsAsync(id, _nextPage, PageSize);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить историю.";
                return;
            }

            foreach (var item in envelope.Result.Items)
                Edits.Add(EditEntry.From(item));

            TotalCount = envelope.Result.TotalCount;
            _nextPage++;
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = (int)apiEx.StatusCode switch
            {
                403 => "Просмотр истории доступен только администраторам.",
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
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

/// <summary>
/// VM-обёртка над DTO с уже распарсенным diff. Парсинг ChangesJson в
/// конструкторе — один раз, чтобы XAML просто биндился на коллекцию.
/// </summary>
public sealed class EditEntry
{
    public string EditedAt { get; init; } = "";
    public string Editor { get; init; } = "";
    public string KindDisplay { get; init; } = "";
    public IReadOnlyList<ChangeRow> Changes { get; init; } = Array.Empty<ChangeRow>();

    public static EditEntry From(DeceasedEditDto dto)
    {
        // Backend сериализует enum через JsonStringEnumConverter — приходит
        // строкой "MainInfo"/"Metadata"/"BurialLocation".
        var kindDisplay = dto.Kind switch
        {
            "MainInfo" => "Основное",
            "Metadata" => "Дополнительно",
            "BurialLocation" => "Место захоронения",
            "Reassignment" => "Переуступка (удаление автора)",
            _ => dto.Kind,
        };

        // Reassignment: EditedByUserId всегда NULL (системная операция),
        // email удалённого юзера — в ChangesJson.PreviousAuthor.Old.
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

        return new EditEntry
        {
            EditedAt = dto.EditedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            Editor = editor,
            KindDisplay = kindDisplay,
            Changes = changes,
        };
    }

}

public sealed record ChangeRow(string Field, string Old, string New);
