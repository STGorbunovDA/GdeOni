using GdeOni.Application.Abstractions.Storage;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaContent.Model;

/// <summary>
/// D47. Результат «вахтёра»: поток файла + оригинальное имя (для
/// Content-Disposition). Контроллер сам отдаёт <see cref="DownloadedFile"/>
/// как <c>FileStreamResult</c>.
/// </summary>
public sealed record GetMediaContentResult(DownloadedFile File, string OriginalFileName);
