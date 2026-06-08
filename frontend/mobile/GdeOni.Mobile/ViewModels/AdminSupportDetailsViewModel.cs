using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.ViewModels.Support;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D25 mobile. Админская карточка обращения. Действия: смена статуса
/// (при Resolved обязательно указать ResolutionNote), смена severity,
/// переход на профиль юзера-автора.
/// </summary>
[QueryProperty(nameof(TicketId), "ticketId")]
public partial class AdminSupportDetailsViewModel(ISupportApi supportApi) : ObservableObject
{
    public IReadOnlyList<string> StatusOptions { get; } =
        new[] { "Открыто", "В работе", "Решено" };

    public IReadOnlyList<string> SeverityOptions { get; } =
        new[] { "Обычно", "Срочно" };

    // string, не Guid — Shell.GoToAsync передаёт query-параметры строками,
    // а конвертация в Guid в setter'е вызывала JavaProxyThrowable в Shell
    // pipeline'е на эмуляторе.
    [ObservableProperty]
    private string _ticketId = "";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty] private string? _title;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _kindDisplay;
    [ObservableProperty] private string? _sourceDisplay;
    [ObservableProperty] private string? _createdAt;
    [ObservableProperty] private string? _updatedAt;
    [ObservableProperty] private string? _details;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDetails))]
    private bool _hasDetailsValue;
    public bool HasDetails => HasDetailsValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUser))]
    [NotifyPropertyChangedFor(nameof(UserButtonLabel))]
    private string? _userEmail;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUser))]
    private Guid? _userId;

    public bool HasUser => UserId is not null && UserId != Guid.Empty;
    public string UserButtonLabel => string.IsNullOrEmpty(UserEmail)
        ? "Открыть профиль"
        : $"Профиль: {UserEmail}";

    // ───────── Текущее состояние ─────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResolved))]
    private string? _currentStatusCode;

    [ObservableProperty] private string? _currentStatusDisplay;
    [ObservableProperty] private string _currentStatusColor = "#000";

    [ObservableProperty] private string? _currentSeverityCode;
    [ObservableProperty] private string? _currentSeverityDisplay;
    [ObservableProperty] private string _currentSeverityColor = "#000";

    public bool IsResolved => CurrentStatusCode == "Resolved";

    // ───────── Резолюция ─────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResolution))]
    private string? _resolutionNote;
    [ObservableProperty] private string? _resolvedAt;
    public bool HasResolution => !string.IsNullOrWhiteSpace(ResolutionNote);

    // ───────── Picker'ы для смены ─────────
    [ObservableProperty] private string _selectedStatusOption = "Открыто";
    [ObservableProperty] private string _selectedSeverityOption = "Обычно";

    /// <summary>
    /// Note, который админ вводит при смене статуса на Resolved.
    /// Если выбранный статус не Resolved — не используется.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResolveSelected))]
    private string _newResolutionNote = "";

    public bool IsResolveSelected => SelectedStatusOption == "Решено";

    partial void OnSelectedStatusOptionChanged(string value)
    {
        OnPropertyChanged(nameof(IsResolveSelected));
    }

    partial void OnTicketIdChanged(string value)
    {
        if (!Guid.TryParse(value, out _)) return;
        // Откладываем на следующий тик main thread'а — см. комментарий
        // в SupportDetailsViewModel: ANR-фикс для случая, когда
        // Shell-навигация и HTTP-запрос конфликтуют на стартовом frame.
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!Guid.TryParse(TicketId, out var id) || id == Guid.Empty) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var envelope = await supportApi.GetAdminByIdAsync(id);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить обращение.";
                return;
            }

            var t = envelope.Result.Ticket;
            Title = t.Title;
            Description = t.Description;
            KindDisplay = SupportDisplay.Kind(t.Kind);
            SourceDisplay = SupportDisplay.Source(t.Source);
            CreatedAt = t.CreatedAtUtc.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            UpdatedAt = t.UpdatedAtUtc?.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

            UserId = t.UserId;
            UserEmail = t.UserEmail;

            Details = t.Details;
            HasDetailsValue = !string.IsNullOrWhiteSpace(t.Details);

            CurrentStatusCode = t.Status;
            var (sd, sc) = SupportDisplay.Status(t.Status);
            CurrentStatusDisplay = sd;
            CurrentStatusColor = sc;
            SelectedStatusOption = sd;

            CurrentSeverityCode = t.Severity;
            var (sevd, sevc) = SupportDisplay.Severity(t.Severity);
            CurrentSeverityDisplay = sevd;
            CurrentSeverityColor = sevc;
            SelectedSeverityOption = sevd;

            ResolutionNote = t.ResolutionNote;
            ResolvedAt = t.ResolvedAtUtc?.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            NewResolutionNote = "";
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasUser));
            OnPropertyChanged(nameof(HasResolution));
            OnPropertyChanged(nameof(IsResolved));
        }
    }

    [RelayCommand]
    private async Task SaveStatusAsync()
    {
        if (IsSaving) return;
        var newCode = MapStatusLabel(SelectedStatusOption);
        if (newCode is null) return;

        // На Resolved обязательно указать причину.
        if (newCode == "Resolved" && string.IsNullOrWhiteSpace(NewResolutionNote))
        {
            await Shell.Current.DisplayAlert(
                "Нужна причина",
                "Чтобы закрыть обращение, опишите что было сделано.",
                "ОК");
            return;
        }

        if (!Guid.TryParse(TicketId, out var id)) return;
        try
        {
            IsSaving = true;
            ErrorMessage = null;
            var resp = await supportApi.UpdateStatusAsync(
                id,
                new UpdateSupportTicketStatusRequest(
                    newCode,
                    newCode == "Resolved" ? NewResolutionNote.Trim() : null));

            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}";
                return;
            }

            await LoadAsync();
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task SaveSeverityAsync()
    {
        if (IsSaving) return;
        var newCode = MapSeverityLabel(SelectedSeverityOption);
        if (newCode is null) return;

        if (!Guid.TryParse(TicketId, out var id)) return;
        try
        {
            IsSaving = true;
            ErrorMessage = null;
            var resp = await supportApi.UpdateSeverityAsync(
                id,
                new UpdateSupportTicketSeverityRequest(newCode));

            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}";
                return;
            }

            await LoadAsync();
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task OpenUserAsync()
    {
        if (UserId is not { } id || id == Guid.Empty) return;
        // Переиспользуем существующую админ-страницу профиля юзера
        // (F17.7) — её параметр уже называется userId.
        await Shell.Current.GoToAsync($"admin-user-details?userId={id}");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    private static string? MapStatusLabel(string label) => label switch
    {
        "Открыто" => "Open",
        "В работе" => "InProgress",
        "Решено" => "Resolved",
        _ => null,
    };

    private static string? MapSeverityLabel(string label) => label switch
    {
        "Обычно" => "Normal",
        "Срочно" => "Urgent",
        _ => null,
    };
}
