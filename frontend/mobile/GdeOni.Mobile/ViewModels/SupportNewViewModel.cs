using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D25 mobile + D33. Форма создания обращения. Юзер выбирает тематику,
/// пишет заголовок и описание, опционально прикладывает до 5 файлов
/// (JPEG/PNG до 10MB или PDF до 25MB, суммарно ≤50MB).
/// Severity всегда Normal — апгрейдить может только админ.
///
/// При Submit:
///  — если вложений нет → старая ручка POST /api/support-tickets (JSON);
///  — иначе → POST /api/support-tickets/with-attachments (multipart).
///
/// D34. Если открыли с карточки умершего (через
/// OpenSupportFromDeceasedCommand), query-параметры
/// deceasedId/deceasedFullName/deceasedLifePeriod подставляются в
/// готовый шаблон Description. Маркер "ID карточки: {guid}" админ
/// потом распознаёт в AdminSupportDetailsViewModel и открывает
/// карточку одним тапом.
/// </summary>
[QueryProperty(nameof(DeceasedId), "deceasedId")]
[QueryProperty(nameof(DeceasedFullName), "deceasedFullName")]
[QueryProperty(nameof(DeceasedLifePeriod), "deceasedLifePeriod")]
[QueryProperty(nameof(PresetKind), "kind")]
public partial class SupportNewViewModel(ISupportApi supportApi) : ObservableObject
{
    // D34. Маркер, по которому админ потом находит deceasedId
    // в Description. Менять формат — синхронно с
    // SupportDeceasedRefParser.
    public const string DeceasedIdMarker = "ID карточки:";

    [ObservableProperty] private string? _deceasedId;
    [ObservableProperty] private string? _deceasedFullName;
    [ObservableProperty] private string? _deceasedLifePeriod;

    partial void OnDeceasedIdChanged(string? value) => TryApplyDeceasedTemplate();
    partial void OnDeceasedFullNameChanged(string? value) => TryApplyDeceasedTemplate();
    partial void OnDeceasedLifePeriodChanged(string? value) => TryApplyDeceasedTemplate();

    /// <summary>
    /// D44. Тема, заданная извне через <c>?kind=</c>. Приходит с paywall
    /// со значением Payment: человеку, которого отрезало от приложения,
    /// не надо ещё и выбирать тему и формулировать текст.
    /// </summary>
    [ObservableProperty] private string? _presetKind;

    partial void OnPresetKindChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var option = KindOptions.FirstOrDefault(
            k => string.Equals(k.Value, value, StringComparison.OrdinalIgnoreCase));
        if (option is null) return;

        SelectedKind = option;

        // Заголовок и текст доподставляем только для сценария оплаты и
        // только если юзер ещё ничего не ввёл.
        if (!string.Equals(option.Value, "Payment", StringComparison.Ordinal)) return;
        if (_templateApplied) return;

        if (string.IsNullOrWhiteSpace(Title))
            Title = "Оплата подписки";

        if (string.IsNullOrWhiteSpace(Description))
        {
            Description =
                "Здравствуйте! Пробный период закончился, хочу продлить доступ.\n" +
                "Подскажите, пожалуйста, как оплатить.\n";
        }

        _templateApplied = true;
    }

    private bool _templateApplied;

    /// <summary>
    /// D34. Когда все query-параметры пришли, один раз заполняем
    /// готовый шаблон в Description. Юзеру остаётся выбрать тему
    /// и дописать суть проблемы после "Опишите проблему ниже:".
    /// </summary>
    private void TryApplyDeceasedTemplate()
    {
        if (_templateApplied) return;
        if (string.IsNullOrWhiteSpace(DeceasedId)) return;
        if (string.IsNullOrWhiteSpace(DeceasedFullName)) return;

        var period = !string.IsNullOrWhiteSpace(DeceasedLifePeriod)
            ? $"\nЖизнь: {DeceasedLifePeriod}"
            : "";

        Description =
            $"Карточка умершего: {DeceasedFullName}{period}\n" +
            $"{DeceasedIdMarker} {DeceasedId}\n" +
            "\n" +
            "---\n" +
            "\n" +
            "Опишите проблему ниже:\n";

        Title = $"По карточке: {DeceasedFullName}";
        _templateApplied = true;
    }


    private const int MaxAttachments = 5;
    private const long MaxPhotoBytes = 10L * 1024 * 1024;
    private const long MaxPdfBytes = 25L * 1024 * 1024;
    private const long MaxTotalBytes = 50L * 1024 * 1024;

    /// <summary>
    /// Локализованные подписи для UI + кодовое значение для бэка.
    /// Совпадает со строковыми значениями enum SupportTicketKind.
    /// D33: добавлена тема "Фото" (бэк: Photo).
    /// </summary>
    public IReadOnlyList<KindOption> KindOptions { get; } = new[]
    {
        new KindOption("Платёж", "Payment"),
        new KindOption("Ошибка / Баг", "Bug"),
        new KindOption("Жалоба", "Complaint"),
        new KindOption("Вопрос", "Question"),
        new KindOption("Фото", "Photo"),
        new KindOption("Другое", "Other"),
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private KindOption? _selectedKind;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _title = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _description = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyCanExecuteChangedFor(nameof(PickAttachmentCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>
    /// D33. Список приложенных файлов (фото/PDF). При создании тикета
    /// пакуем их в multipart-форму.
    /// </summary>
    public ObservableCollection<PickedAttachment> Attachments { get; } = new();

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string AttachmentsLabel => $"Прикрепить файл ({Attachments.Count}/{MaxAttachments})";

    public bool HasAttachments => Attachments.Count > 0;

    public bool CanPickAttachment => !IsBusy && Attachments.Count < MaxAttachments;

    public bool CanSubmit =>
        !IsBusy
        && SelectedKind is not null
        && !string.IsNullOrWhiteSpace(Title)
        && !string.IsNullOrWhiteSpace(Description);

    [RelayCommand(CanExecute = nameof(CanPickAttachment))]
    private async Task PickAttachmentAsync()
    {
        try
        {
            ErrorMessage = null;

            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "image/jpeg", "image/png", "application/pdf" },
                    [DevicePlatform.iOS] = new[] { "public.jpeg", "public.png", "com.adobe.pdf" },
                    [DevicePlatform.WinUI] = new[] { ".jpg", ".jpeg", ".png", ".pdf" },
                    [DevicePlatform.MacCatalyst] = new[] { "public.jpeg", "public.png", "com.adobe.pdf" },
                });

            var picked = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = customFileType,
                PickerTitle = "Выберите фото или PDF",
            });
            if (picked is null) return;

            // Считываем сразу в byte[] — MAUI FilePicker отдаёт временный
            // поток, который закроется до момента отправки тикета.
            await using var src = await picked.OpenReadAsync();
            using var ms = new MemoryStream();
            await src.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var contentType = NormalizeContentType(picked.ContentType, picked.FileName);

            // Локальная валидация — fail-fast до отправки.
            if (contentType is not ("image/jpeg" or "image/png" or "application/pdf"))
            {
                ErrorMessage = "Только JPEG, PNG или PDF.";
                return;
            }

            var perFileLimit = contentType == "application/pdf" ? MaxPdfBytes : MaxPhotoBytes;
            if (bytes.LongLength > perFileLimit)
            {
                ErrorMessage = contentType == "application/pdf"
                    ? "PDF слишком большой (макс. 25 MB)."
                    : "Фото слишком большое (макс. 10 MB).";
                return;
            }

            var totalAfter = Attachments.Sum(a => (long)a.Bytes.LongLength) + bytes.LongLength;
            if (totalAfter > MaxTotalBytes)
            {
                ErrorMessage = "Суммарный размер вложений не должен превышать 50 MB.";
                return;
            }

            Attachments.Add(new PickedAttachment(
                FileName: picked.FileName,
                ContentType: contentType,
                Bytes: bytes));

            OnPropertyChanged(nameof(AttachmentsLabel));
            OnPropertyChanged(nameof(HasAttachments));
            OnPropertyChanged(nameof(CanPickAttachment));
            PickAttachmentCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось приложить файл: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveAttachment(PickedAttachment attachment)
    {
        if (attachment is null) return;
        Attachments.Remove(attachment);
        OnPropertyChanged(nameof(AttachmentsLabel));
        OnPropertyChanged(nameof(CanPickAttachment));
        PickAttachmentCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (SelectedKind is null) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Guid ticketId;
            if (Attachments.Count == 0)
            {
                // Без вложений — старая ручка, без multipart-overhead'а.
                var envelope = await supportApi.CreateAsync(
                    new CreateSupportTicketRequest(
                        SelectedKind.Value,
                        Title.Trim(),
                        Description.Trim()));

                if (envelope.Result is null)
                {
                    ErrorMessage = envelope.ErrorMessage ?? "Не удалось отправить обращение.";
                    return;
                }
                ticketId = envelope.Result.TicketId;
            }
            else
            {
                var parts = Attachments
                    .Select(a => new StreamPart(
                        new MemoryStream(a.Bytes),
                        a.FileName,
                        a.ContentType))
                    .ToList();

                var envelope = await supportApi.CreateWithAttachmentsAsync(
                    SelectedKind.Value,
                    Title.Trim(),
                    Description.Trim(),
                    parts);

                if (envelope.Result is null)
                {
                    ErrorMessage = envelope.ErrorMessage ?? "Не удалось отправить обращение.";
                    return;
                }
                ticketId = envelope.Result.TicketId;
            }

            await Shell.Current.DisplayAlert(
                "Готово",
                "Ваше обращение отправлено. Ответ появится в разделе \"Мои обращения\".",
                "ОК");

            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync("support-mine");
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
            IsBusy = false;
            SubmitCommand.NotifyCanExecuteChanged();
            PickAttachmentCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    private static string NormalizeContentType(string? raw, string? fileName)
    {
        // На некоторых Android-устройствах FilePicker возвращает пустой
        // ContentType. Восстанавливаем по расширению.
        if (!string.IsNullOrWhiteSpace(raw)) return raw;

        var ext = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => raw ?? string.Empty,
        };
    }
}

public sealed record KindOption(string Display, string Value)
{
    public override string ToString() => Display;
}

/// <summary>D33. Выбранное юзером вложение до отправки тикета.</summary>
public sealed record PickedAttachment(string FileName, string ContentType, byte[] Bytes)
{
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public string SizeLabel
    {
        get
        {
            var mb = Bytes.LongLength / (1024.0 * 1024.0);
            return mb >= 0.1 ? $"{mb:0.0} MB" : $"{Bytes.LongLength / 1024} KB";
        }
    }
}
