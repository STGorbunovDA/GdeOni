using GdeOni.Mobile.Services.Api.Models;

namespace GdeOni.Mobile.ViewModels.Support;

/// <summary>
/// D33. UI-обёртка над <see cref="SupportTicketAttachmentDto"/>: метаданные
/// + флаги для шаблона (IsImage / SizeLabel). URL клиент получает по
/// требованию (тап → GetAttachmentAsync → открыть в браузере).
/// </summary>
public sealed record AttachmentDisplayItem(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes)
{
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public string SizeLabel
    {
        get
        {
            var mb = SizeBytes / (1024.0 * 1024.0);
            return mb >= 0.1 ? $"{mb:0.0} MB" : $"{SizeBytes / 1024} KB";
        }
    }

    public static AttachmentDisplayItem From(SupportTicketAttachmentDto dto) =>
        new(dto.Id, dto.OriginalFileName, dto.ContentType, dto.SizeBytes);
}
