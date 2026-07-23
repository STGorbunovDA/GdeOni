using GdeOni.Application.Abstractions.Storage;

namespace GdeOni.Application.DeceasedRecords.Queries.DownloadMedia.Model;

/// <summary>
/// Готовый поток для стриминга клиенту + метаданные для HTTP-заголовков.
/// File.Content нужно задиспозить после стриминга — это ответственность
/// controller'а (ASP.NET сам делает Dispose у FileStreamResult).
/// </summary>
public sealed record DownloadMediaResult(
    DownloadedFile File,
    string OriginalFileName);
