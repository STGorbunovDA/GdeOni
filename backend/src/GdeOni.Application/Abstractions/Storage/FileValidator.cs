using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Storage;

public static class FileValidator
{
    public const long MaxPhotoSizeBytes = 10L * 1024 * 1024;
    public const long MaxDocumentSizeBytes = 25L * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedPhotoContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    public static readonly IReadOnlySet<string> AllowedDocumentContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf"
        };

    // MediaKind.Other закрыт на upload (D7.69): ValidateAny не имел
    // ни allow-list, ни magic-bytes — позволял аутентифицированному
    // юзеру загружать произвольные бинарники. Сам enum-value сохранён
    // для совместимости с потенциально существующими записями в БД,
    // но на запись путь блокирован через KindInvalid.
    public static UnitResult<Error> ValidateForKind(MediaKind kind, string contentType, long sizeBytes) =>
        kind switch
        {
            MediaKind.DeceasedPhoto or MediaKind.GravePhoto => ValidatePhoto(contentType, sizeBytes),
            MediaKind.Document => ValidateDocument(contentType, sizeBytes),
            _ => Errors.DeceasedMedia.KindInvalid()
        };

    public static UnitResult<Error> ValidatePhoto(string contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return Errors.DeceasedMedia.ContentTypeRequired();

        if (!AllowedPhotoContentTypes.Contains(contentType))
            return Errors.Media.PhotoContentTypeNotAllowed(contentType);

        if (sizeBytes <= 0)
            return Errors.DeceasedMedia.SizeBytesInvalid();

        if (sizeBytes > MaxPhotoSizeBytes)
            return Errors.Media.PhotoTooLarge(MaxPhotoSizeBytes);

        return UnitResult.Success<Error>();
    }

    public static UnitResult<Error> ValidateDocument(string contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return Errors.DeceasedMedia.ContentTypeRequired();

        if (!AllowedDocumentContentTypes.Contains(contentType))
            return Errors.Media.DocumentContentTypeNotAllowed(contentType);

        if (sizeBytes <= 0)
            return Errors.DeceasedMedia.SizeBytesInvalid();

        if (sizeBytes > MaxDocumentSizeBytes)
            return Errors.Media.DocumentTooLarge(MaxDocumentSizeBytes);

        return UnitResult.Success<Error>();
    }

    private const int MagicBytesProbeLength = 12;

    public static async Task<UnitResult<Error>> ValidateMagicBytesAsync(
        Stream content,
        string contentType,
        MediaKind kind,
        CancellationToken cancellationToken)
    {
        if (content is null || !content.CanRead || !content.CanSeek)
            return Errors.Media.UnreadableStream();

        if (string.IsNullOrWhiteSpace(contentType))
            return Errors.DeceasedMedia.ContentTypeRequired();

        var buffer = new byte[MagicBytesProbeLength];
        var originalPosition = content.Position;
        content.Position = 0;
        var read = await ReadFullyAsync(content, buffer, cancellationToken);
        content.Position = originalPosition;

        if (read < MagicBytesProbeLength || !MatchesSignature(buffer, contentType))
            return Errors.Media.MagicBytesMismatch(contentType);

        return UnitResult.Success<Error>();
    }

    private static async Task<int> ReadFullyAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(
                buffer.AsMemory(total, buffer.Length - total),
                cancellationToken);
            if (read == 0) break;
            total += read;
        }
        return total;
    }

    private static bool MatchesSignature(ReadOnlySpan<byte> head, string contentType)
    {
        if (string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
            return head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF;

        if (string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
            return head.Length >= 8
                   && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47
                   && head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A;

        if (string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase))
            return head.Length >= 12
                   && head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46
                   && head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50;

        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return head.Length >= 4 && head[0] == 0x25 && head[1] == 0x50 && head[2] == 0x44 && head[3] == 0x46;

        return false;
    }
}

public static class MediaConstants
{
    public static readonly TimeSpan DocumentPresignedUrlTtl = TimeSpan.FromHours(1);
}
