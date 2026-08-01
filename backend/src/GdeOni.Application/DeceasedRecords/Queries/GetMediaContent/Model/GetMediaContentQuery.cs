namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaContent.Model;

/// <summary>
/// D47. Запрос на стрим файла через «вахтёра». Только mediaId — маршрут
/// плоский (<c>/api/media/{id}/content</c>), без deceasedId: клиент
/// получает готовый путь в поле url/photoUrl и не собирает его сам.
/// </summary>
public sealed record GetMediaContentQuery(Guid MediaId);
