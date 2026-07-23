using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using GdeOni.Mobile.ViewModels.Support;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D25 mobile. Карточка обращения с точки зрения юзера. Если тикет
/// Resolved и юзер ещё не закрепил — показываются две кнопки
/// "Закрепить решено" и "Продолжить спор".
/// </summary>
[QueryProperty(nameof(TicketId), "ticketId")]
public partial class SupportDetailsViewModel(ISupportApi supportApi) : ObservableObject
{
    // string, не Guid — Shell передаёт query-параметры строками.
    [ObservableProperty]
    private string _ticketId = "";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty] private string? _title;
    [ObservableProperty] private string? _kindDisplay;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _createdAt;

    [ObservableProperty] private string? _statusDisplay;
    [ObservableProperty] private string _statusColor = "#000";

    [ObservableProperty] private string? _severityDisplay;
    [ObservableProperty] private string _severityColor = "#000";

    // Резолюция админа
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResolution))]
    private string? _resolutionNote;
    [ObservableProperty] private string? _resolvedAt;
    public bool HasResolution => !string.IsNullOrWhiteSpace(ResolutionNote);

    // Статусы юзерских действий
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowActionButtons))]
    [NotifyPropertyChangedFor(nameof(ShowAcceptedNote))]
    private bool _acceptedByUser;
    [ObservableProperty] private string? _acceptedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowActionButtons))]
    private bool _isResolved;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReopenedHistory))]
    [NotifyPropertyChangedFor(nameof(ReopenedHistoryText))]
    private int _reopenedCount;

    // Моё последнее сообщение админу из "Продолжить спор". Показываю
    // юзеру тоже — чтобы он видел, что именно отправил, и не пытался
    // вспомнить по памяти.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastUserReply))]
    private string? _lastUserReply;
    [ObservableProperty] private string? _lastUserReplyAt;
    public bool HasLastUserReply => !string.IsNullOrWhiteSpace(LastUserReply);

    /// <summary>
    /// D25.2. Полная переписка в хронологии ASC. Юзер видит свои
    /// сообщения справа (белые), админа слева (жёлтые).
    /// </summary>
    public ObservableCollection<ChatMessage> ChatMessages { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChatMessages))]
    private int _chatMessagesCount;
    public bool HasChatMessages => ChatMessagesCount > 0;

    /// <summary>D33. Вложения, которые юзер приложил при создании тикета.</summary>
    public ObservableCollection<AttachmentDisplayItem> Attachments { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAttachments))]
    private int _attachmentsCount;
    public bool HasAttachments => AttachmentsCount > 0;

    public bool HasReopenedHistory => ReopenedCount > 0;
    public string ReopenedHistoryText => ReopenedCount switch
    {
        0 => string.Empty,
        1 => "Обращение переоткрывалось 1 раз",
        var n => $"Обращение переоткрывалось {n} раз(а)",
    };

    /// <summary>
    /// Кнопки "Закрепить решено" / "Продолжить спор" показываем только
    /// если тикет уже Resolved и юзер ещё не закрепил резолюцию.
    /// </summary>
    public bool CanShowActionButtons => IsResolved && !AcceptedByUser;

    /// <summary>Юзер уже закрепил — показываем плашку "Вы подтвердили решение".</summary>
    public bool ShowAcceptedNote => AcceptedByUser;

    /// <summary>
    /// D44. Можно ли писать в переписку. Только Open / InProgress:
    /// на Resolved есть отдельные кнопки (закрепить / продолжить спор),
    /// на Closed переписка окончена.
    /// </summary>
    [ObservableProperty]
    private bool _canWriteMessage;

    /// <summary>D44. Текст нового сообщения.</summary>
    [ObservableProperty]
    private string _newMessageText = "";

    partial void OnTicketIdChanged(string value)
    {
        if (!Guid.TryParse(value, out _)) return;
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
            var envelope = await supportApi.GetMineByIdAsync(id);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить обращение.";
                return;
            }

            ApplyDto(envelope.Result.Ticket);
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
        }
    }

    /// <summary>Юзер тапнул "Закрепить решено".</summary>
    [RelayCommand]
    private async Task AcceptAsync()
    {
        if (IsSaving) return;
        if (!Guid.TryParse(TicketId, out var id)) return;

        var ok = await Shell.Current.DisplayAlert(
            "Закрепить решение",
            "Подтвердить, что проблема решена? После этого спорить будет нельзя.",
            "Закрепить", "Отмена");
        if (!ok) return;

        try
        {
            IsSaving = true;
            ErrorMessage = null;
            var resp = await supportApi.AcceptAsync(id);
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

    /// <summary>
    /// Юзер тапнул "Продолжить спор". Просим ввести сообщение —
    /// без него тикет тоже переоткроется (бэк позволяет), но
    /// админу полезнее видеть контекст, поэтому UI требует текст.
    /// </summary>
    [RelayCommand]
    private async Task ReopenAsync()
    {
        if (IsSaving) return;
        if (!Guid.TryParse(TicketId, out var id)) return;

        var reply = await Shell.Current.DisplayPromptAsync(
            "Продолжить спор",
            "Опишите, почему ответ не подходит. Сообщение увидит администратор.",
            accept: "Отправить",
            cancel: "Отмена",
            placeholder: "Что не так с ответом?",
            maxLength: 4000,
            keyboard: Keyboard.Default);
        if (string.IsNullOrWhiteSpace(reply)) return;

        try
        {
            IsSaving = true;
            ErrorMessage = null;
            var resp = await supportApi.ReopenAsync(
                id, new ReopenSupportTicketRequest(reply.Trim()));
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

    /// <summary>
    /// D44. Обычное сообщение в переписку — отдельно от «Продолжить
    /// спор»: тот доступен только на Resolved и увеличивает счётчик
    /// переоткрытий, а реплика нужна как раз пока обращение в работе.
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (IsSaving) return;
        if (!Guid.TryParse(TicketId, out var id)) return;

        var text = NewMessageText?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            IsSaving = true;
            ErrorMessage = null;

            var resp = await supportApi.AddMessageAsync(
                id, new AddSupportTicketMessageRequest(text));
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}";
                return;
            }

            NewMessageText = "";
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
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    /// <summary>
    /// D33+D35. Открыть вложение:
    /// — фото → fullscreen-просмотр (photo-viewer);
    /// — PDF → inline-просмотр внутри приложения (pdf-preview);
    /// — прочее → системный Launcher (fallback).
    /// </summary>
    [RelayCommand]
    private async Task OpenAttachmentAsync(AttachmentDisplayItem? item)
    {
        if (item is null) return;
        if (!Guid.TryParse(TicketId, out var tid)) return;
        try
        {
            var envelope = await supportApi.GetAttachmentAsync(tid, item.Id);
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось получить файл.";
                return;
            }
            await OpenByContentTypeAsync(item, envelope.Result.PresignedUrl);
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
    }

    private static async Task OpenByContentTypeAsync(AttachmentDisplayItem item, string presignedUrl)
    {
        // Фото — fullscreen-просмотр внутри приложения.
        if ((item.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var encodedUrl = Uri.EscapeDataString(presignedUrl);
            await Shell.Current.GoToAsync($"photo-viewer?url={encodedUrl}");
            return;
        }

        // PDF и прочее — системный Launcher. Android WebView не умеет
        // рендерить PDF inline (это не desktop Chrome), а проксировать
        // через Google Docs Viewer нельзя из-за приватности (в тикетах
        // могут быть паспортные данные / свидетельства). Юзер выходит
        // в системный PDF-вьюер, но это безопасно.
        await Launcher.OpenAsync(new Uri(presignedUrl));
    }

    private void ApplyDto(SupportTicketDto t)
    {
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

        IsResolved = t.Status == "Resolved";
        // D44. Зеркало доменного правила SupportTicket.AddUserMessage:
        // писать можно, пока обращение в работе.
        CanWriteMessage = t.Status is "Open" or "InProgress";
        AcceptedByUser = t.AcceptedByUser;
        AcceptedAt = t.AcceptedByUserAtUtc?.ToLocalTime()
            .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        ReopenedCount = t.ReopenedCount;
        LastUserReply = t.LastUserReply;
        LastUserReplyAt = t.LastUserReplyAtUtc?.ToLocalTime()
            .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

        // D25.2. Перестраиваем чат с нуля каждый раз — это надёжнее
        // чем диффы и для десятка сообщений дешево.
        ChatMessages.Clear();
        if (t.Messages is not null)
        {
            foreach (var msg in t.Messages)
                ChatMessages.Add(ChatMessage.ForViewer(msg, viewerIsAdmin: false));
        }
        ChatMessagesCount = ChatMessages.Count;

        // D33. Перестраиваем список вложений.
        Attachments.Clear();
        if (t.Attachments is not null)
        {
            foreach (var att in t.Attachments)
                Attachments.Add(AttachmentDisplayItem.From(att));
        }
        AttachmentsCount = Attachments.Count;
    }
}
