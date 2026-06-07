using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.ViewModels.Support;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D25 mobile. Карточка обращения с точки зрения юзера. Read-only —
/// никаких действий (Resolved + note выставляет админ из своей
/// карточки).
/// </summary>
[QueryProperty(nameof(TicketId), "ticketId")]
public partial class SupportDetailsViewModel(ISupportApi supportApi) : ObservableObject
{
    [ObservableProperty]
    private Guid _ticketId;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResolved))]
    private string? _title;

    [ObservableProperty] private string? _kindDisplay;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _createdAt;

    [ObservableProperty] private string? _statusDisplay;
    [ObservableProperty] private string _statusColor = "#000";

    [ObservableProperty] private string? _severityDisplay;
    [ObservableProperty] private string _severityColor = "#000";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResolved))]
    [NotifyPropertyChangedFor(nameof(HasResolution))]
    private string? _resolutionNote;

    [ObservableProperty] private string? _resolvedAt;

    public bool IsResolved => StatusDisplay == "Решено";
    public bool HasResolution => !string.IsNullOrWhiteSpace(ResolutionNote);

    partial void OnTicketIdChanged(Guid value)
    {
        if (value != Guid.Empty)
            _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (TicketId == Guid.Empty) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var envelope = await supportApi.GetMineByIdAsync(TicketId);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить обращение.";
                return;
            }

            var t = envelope.Result.Ticket;
            Title = t.Title;
            Description = t.Description;
            KindDisplay = SupportDisplay.Kind(t.Kind);
            CreatedAt = t.CreatedAtUtc.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

            var (sd, sc) = SupportDisplay.Status(t.Status);
            StatusDisplay = sd; StatusColor = sc;

            var (sevd, sevc) = SupportDisplay.Severity(t.Severity);
            SeverityDisplay = sevd; SeverityColor = sevc;

            ResolutionNote = t.ResolutionNote;
            ResolvedAt = t.ResolvedAtUtc?.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
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
            OnPropertyChanged(nameof(IsResolved));
            OnPropertyChanged(nameof(HasResolution));
        }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}
