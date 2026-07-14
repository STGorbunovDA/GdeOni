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
    [NotifyPropertyChangedFor(nameof(IsClosed))]
    [NotifyPropertyChangedFor(nameof(CanForceClose))]
    [NotifyPropertyChangedFor(nameof(CanEditStatus))]
    private string? _currentStatusCode;

    [ObservableProperty] private string? _currentStatusDisplay;
    [ObservableProperty] private string _currentStatusColor = "#000";

    [ObservableProperty] private string? _currentSeverityCode;
    [ObservableProperty] private string? _currentSeverityDisplay;
    [ObservableProperty] private string _currentSeverityColor = "#000";

    public bool IsResolved => CurrentStatusCode == "Resolved";

    /// <summary>
    /// D40. Закрыто принудительно — терминальное состояние: ни статус, ни
    /// severity уже не поменять, юзер переоткрыть не может.
    /// </summary>
    public bool IsClosed => CurrentStatusCode == "Closed";

    /// <summary>Кнопка «Закрыть принудительно» активна для любого статуса, кроме Closed.</summary>
    public bool CanForceClose => !IsClosed;

    /// <summary>
    /// Можно ли менять статус/приоритет. Бэк запрещает это только на
    /// Resolved (там точку ставит юзер). На Closed — можно: админ мог
    /// закрыть по ошибке и должен уметь вернуть обращение в работу.
    /// </summary>
    public bool CanEditStatus => !IsResolved;

    // ───────── Резолюция ─────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResolution))]
    private string? _resolutionNote;
    [ObservableProperty] private string? _resolvedAt;
    public bool HasResolution => !string.IsNullOrWhiteSpace(ResolutionNote);

    // ───────── D25.1. Юзерские действия (Accept / Reopen) ─────────
    [ObservableProperty] private bool _acceptedByUser;
    [ObservableProperty] private string? _acceptedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastUserReply))]
    private string? _lastUserReply;
    [ObservableProperty] private string? _lastUserReplyAt;
    public bool HasLastUserReply => !string.IsNullOrWhiteSpace(LastUserReply);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReopenedHistory))]
    [NotifyPropertyChangedFor(nameof(ReopenedHistoryText))]
    private int _reopenedCount;

    /// <summary>D25.2. Полная переписка для админа.</summary>
    public System.Collections.ObjectModel.ObservableCollection<Support.ChatMessage> ChatMessages { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChatMessages))]
    private int _chatMessagesCount;
    public bool HasChatMessages => ChatMessagesCount > 0;

    /// <summary>D33. Вложения тикета — фото и PDF от юзера.</summary>
    public System.Collections.ObjectModel.ObservableCollection<Support.AttachmentDisplayItem> Attachments { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAttachments))]
    private int _attachmentsCount;
    public bool HasAttachments => AttachmentsCount > 0;

    /// <summary>
    /// D34. Если тикет создан с карточки умершего — в Description
    /// есть маркер "ID карточки: {guid}". Парсим и показываем
    /// кнопку быстрого перехода в карточку.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDeceasedRef))]
    private Guid? _deceasedRefId;
    public bool HasDeceasedRef => DeceasedRefId is not null;
    public bool HasReopenedHistory => ReopenedCount > 0;
    public string ReopenedHistoryText => ReopenedCount switch
    {
        0 => string.Empty,
        1 => "↻ Переоткрыто 1 раз юзером",
        var n => $"↻ Переоткрыто {n} раз(а) юзером",
    };

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

            AcceptedByUser = t.AcceptedByUser;
            AcceptedAt = t.AcceptedByUserAtUtc?.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            LastUserReply = t.LastUserReply;
            LastUserReplyAt = t.LastUserReplyAtUtc?.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            ReopenedCount = t.ReopenedCount;

            // D25.2. Чат для админа: свои (Admin) справа белые, юзера
            // (User) слева жёлтые — наоборот по сравнению с юзерским
            // экраном, чтобы каждый видел собеседника подсвеченным.
            ChatMessages.Clear();
            if (t.Messages is not null)
            {
                foreach (var msg in t.Messages)
                    ChatMessages.Add(Support.ChatMessage.ForViewer(msg, viewerIsAdmin: true));
            }
            ChatMessagesCount = ChatMessages.Count;

            // D33. Вложения тикета.
            Attachments.Clear();
            if (t.Attachments is not null)
            {
                foreach (var att in t.Attachments)
                    Attachments.Add(Support.AttachmentDisplayItem.From(att));
            }
            AttachmentsCount = Attachments.Count;

            // D34. Распознаём ссылку на карточку умершего в Description.
            DeceasedRefId = Support.SupportDeceasedRefParser.TryExtract(t.Description);
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
            OnPropertyChanged(nameof(HasLastUserReply));
            OnPropertyChanged(nameof(HasReopenedHistory));
            OnPropertyChanged(nameof(ReopenedHistoryText));
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

    /// <summary>
    /// D40. Закрыть обращение принудительно.
    ///
    /// Нужно, когда юзер забыл подтвердить решение: Resolved не терминален,
    /// точку в нём ставит он сам, и без этого обращение висит вечно.
    /// Причина обязательна — уходит юзеру в переписку, чтобы обращение не
    /// исчезло у него молча.
    /// </summary>
    [RelayCommand]
    private async Task ForceCloseAsync()
    {
        if (IsSaving) return;
        if (!Guid.TryParse(TicketId, out var id)) return;

        if (IsClosed)
        {
            await Shell.Current.DisplayAlert(
                "Уже закрыто",
                "Обращение закрыто принудительно — это конечное состояние.",
                "ОК");
            return;
        }

        var note = await Shell.Current.DisplayPromptAsync(
            "Закрыть принудительно",
            "Причина закрытия — её увидит пользователь в переписке.",
            accept: "Закрыть",
            cancel: "Отмена",
            placeholder: "Например: пользователь не ответил",
            maxLength: 4000);

        // null = юзер нажал «Отмена». Пустая строка — не причина.
        if (note is null) return;
        if (string.IsNullOrWhiteSpace(note))
        {
            await Shell.Current.DisplayAlert(
                "Нужна причина",
                "Без причины закрыть обращение нельзя — пользователь должен понимать, почему.",
                "ОК");
            return;
        }

        try
        {
            IsSaving = true;
            ErrorMessage = null;

            var resp = await supportApi.ForceCloseAsync(
                id,
                new ForceCloseSupportTicketRequest(note.Trim()));

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

    /// <summary>
    /// D34. Открыть карточку умершего, на которую ссылается тикет.
    /// Если юзер пришёл из карточки умершего — переход одной кнопкой.
    /// </summary>
    [RelayCommand]
    private async Task OpenDeceasedAsync()
    {
        if (DeceasedRefId is not { } id || id == Guid.Empty) return;
        // Используем admin-deceased-view (D27) — админ-режим без
        // требования трекинга, в отличие от обычной карточки.
        await Shell.Current.GoToAsync($"admin-deceased-view?deceasedId={id}");
    }

    /// <summary>
    /// D35. Тап на вложении в админ-просмотре тикета — ActionSheet.
    /// Состав зависит от типа вложения и наличия привязки к карточке:
    ///   Фото без DeceasedRefId:
    ///     - Открыть на весь экран.
    ///   Фото с DeceasedRefId:
    ///     - Открыть на весь экран;
    ///     - Сделать главным фото умершего;
    ///     - Добавить в галерею умершего;
    ///     - Добавить как фото могилы.
    ///   PDF без DeceasedRefId:
    ///     - Открыть (Launcher).
    ///   PDF с DeceasedRefId:
    ///     - Открыть (Launcher);
    ///     - Добавить как документ умершего.
    /// </summary>
    [RelayCommand]
    private async Task PhotoLongPressAsync(Support.AttachmentDisplayItem? item)
    {
        if (item is null) return;

        var options = BuildActionSheetOptions(item);
        var action = await Shell.Current.DisplayActionSheet(
            title: item.FileName,
            cancel: "Отмена",
            destruction: null,
            buttons: options);

        if (string.IsNullOrEmpty(action) || action == "Отмена") return;

        switch (action)
        {
            case "Открыть на весь экран":
            case "Открыть":
                await OpenAttachmentAsync(item);
                break;
            case "Сделать главным фото умершего":
                await CopyToDeceasedAsync(item, "DeceasedPhoto", makeMain: true,
                    confirmText: $"Установить «{item.FileName}» как главное фото умершего?",
                    successText: "Фото установлено как главное.");
                break;
            case "Добавить в галерею умершего":
                await CopyToDeceasedAsync(item, "DeceasedPhoto", makeMain: false,
                    confirmText: $"Добавить «{item.FileName}» в галерею фото умершего?",
                    successText: "Фото добавлено в галерею.");
                break;
            case "Добавить как фото могилы":
                await CopyToDeceasedAsync(item, "GravePhoto", makeMain: false,
                    confirmText: $"Добавить «{item.FileName}» как фото могилы?",
                    successText: "Фото добавлено как фото могилы.");
                break;
            case "Добавить как документ умершего":
                await CopyToDeceasedAsync(item, "Document", makeMain: false,
                    confirmText: $"Добавить «{item.FileName}» как документ умершего?",
                    successText: "Документ добавлен.");
                break;
        }
    }

    private string[] BuildActionSheetOptions(Support.AttachmentDisplayItem item)
    {
        var canCopy = HasDeceasedRef;
        if (item.IsImage)
        {
            return canCopy
                ? new[]
                {
                    "Открыть на весь экран",
                    "Сделать главным фото умершего",
                    "Добавить в галерею умершего",
                    "Добавить как фото могилы",
                }
                : new[] { "Открыть на весь экран" };
        }

        // PDF / прочее. Используем заголовок "Открыть" (а не "на весь
        // экран") — это Launcher, не fullscreen-внутренний.
        return canCopy
            ? new[] { "Открыть", "Добавить как документ умершего" }
            : new[] { "Открыть" };
    }

    private async Task CopyToDeceasedAsync(
        Support.AttachmentDisplayItem item,
        string mediaKind,
        bool makeMain,
        string confirmText,
        string successText)
    {
        if (!Guid.TryParse(TicketId, out var tid)) return;
        if (DeceasedRefId is not { } deceasedId) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Подтверждение",
            confirmText + " Файл останется в этом обращении.",
            "Да", "Отмена");
        if (!confirm) return;

        try
        {
            IsSaving = true;
            ErrorMessage = null;
            var envelope = await supportApi.CopyAttachmentToDeceasedAsync(
                tid, item.Id, deceasedId, mediaKind, makeMain);

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Операция не удалась.";
                return;
            }

            await Shell.Current.DisplayAlert("Готово", successText, "ОК");
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
    /// D33+D35. Открыть вложение тикета:
    /// — фото → fullscreen photo-viewer;
    /// — PDF → inline pdf-preview;
    /// — прочее → системный Launcher.
    /// Long-press на фото отдельной командой даёт ActionSheet
    /// "На весь экран / Сделать главным" (см. PhotoLongPressAsync).
    /// </summary>
    [RelayCommand]
    private async Task OpenAttachmentAsync(Support.AttachmentDisplayItem? item)
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

    private static async Task OpenByContentTypeAsync(Support.AttachmentDisplayItem item, string presignedUrl)
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
        // могут быть паспортные данные / свидетельства).
        await Launcher.OpenAsync(new Uri(presignedUrl));
    }

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
