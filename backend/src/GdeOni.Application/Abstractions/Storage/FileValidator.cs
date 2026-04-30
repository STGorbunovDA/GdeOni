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

    public static UnitResult<Error> ValidateForKind(MediaKind kind, string contentType, long sizeBytes) =>
        kind switch
        {
            MediaKind.DeceasedPhoto or MediaKind.GravePhoto => ValidatePhoto(contentType, sizeBytes),
            MediaKind.Document => ValidateDocument(contentType, sizeBytes),
            MediaKind.Other => ValidateAny(contentType, sizeBytes),
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

    private static UnitResult<Error> ValidateAny(string contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return Errors.DeceasedMedia.ContentTypeRequired();

        if (sizeBytes <= 0)
            return Errors.DeceasedMedia.SizeBytesInvalid();

        if (sizeBytes > MaxDocumentSizeBytes)
            return Errors.Media.DocumentTooLarge(MaxDocumentSizeBytes);

        return UnitResult.Success<Error>();
    }
}

public static class MediaConstants
{
    public static readonly TimeSpan DocumentPresignedUrlTtl = TimeSpan.FromHours(1);
}
