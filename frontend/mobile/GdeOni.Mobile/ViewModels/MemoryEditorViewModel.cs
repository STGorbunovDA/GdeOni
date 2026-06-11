using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// VM для MemoryEditorPage. Два режима:
/// - Create (DeceasedId передан, MemoryId пустой) — POST;
/// - Edit (передан и DeceasedId, и MemoryId + InitialText) — PUT.
/// После сохранения backend кладёт запись в Pending — юзер вернётся
/// на карточку, увидит свой текст с пометкой "На модерации".
/// </summary>
[QueryProperty(nameof(DeceasedId), "deceasedId")]
[QueryProperty(nameof(MemoryId), "memoryId")]
[QueryProperty(nameof(InitialText), "text")]
public partial class MemoryEditorViewModel(
    IDeceasedMemoriesApi memoriesApi) : ObservableObject
{
    [ObservableProperty]
    private string _deceasedId = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(IsCreateMode))]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private string _memoryId = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharCount))]
    [NotifyPropertyChangedFor(nameof(CharCountText))]
    [NotifyPropertyChangedFor(nameof(IsTooLong))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _text = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>
    /// QueryProperty приходит url-encoded при навигации
    /// (Shell.Current.GoToAsync). При edit-режиме подставим в Text один раз.
    /// </summary>
    public string InitialText
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
                Text = Uri.UnescapeDataString(value);
        }
    }

    public bool IsEditMode => Guid.TryParse(MemoryId, out _);
    public bool IsCreateMode => !IsEditMode;
    public string Title => IsEditMode ? "Редактировать воспоминание" : "Новое воспоминание";
    public string SaveButtonText => IsEditMode ? "Сохранить" : "Опубликовать";
    public int CharCount => Text?.Length ?? 0;
    public string CharCountText => $"{CharCount} / {MemoryLimits.MaxTextLength}";
    public bool IsTooLong => CharCount > MemoryLimits.MaxTextLength;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool CanSave => !string.IsNullOrWhiteSpace(Text) && !IsTooLong;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Guid.TryParse(DeceasedId, out var deceasedId))
        {
            ErrorMessage = "Некорректный идентификатор карточки.";
            return;
        }
        if (!CanSave) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (Guid.TryParse(MemoryId, out var memoryId))
            {
                var envelope = await memoriesApi.UpdateAsync(
                    deceasedId, memoryId, new UpdateMemoryRequest(Text));
                if (envelope.Result is null)
                {
                    ErrorMessage = envelope.ErrorMessage ?? "Не удалось сохранить воспоминание.";
                    return;
                }
            }
            else
            {
                var envelope = await memoriesApi.AddAsync(
                    deceasedId, new AddMemoryRequest(Text));
                if (envelope.Result is null)
                {
                    ErrorMessage = envelope.ErrorMessage ?? "Не удалось создать воспоминание.";
                    return;
                }
            }

            // Возвращаемся на карточку. Карточка перечитает Memories через
            // OnAppearing (см. DeceasedDetailsPage.OnAppearing → LoadAsync).
            await Shell.Current.GoToAsync("..");
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
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
