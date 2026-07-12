using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

/// <summary>
/// События: справочник праздников (backend GET /api/events/holidays).
/// Даты передаём строками ISO yyyy-MM-dd — так backend их однозначно
/// парсит в DateOnly, без зависимости от культуры сериализатора.
/// </summary>
public interface IEventsApi
{
    [Get("/api/events/holidays")]
    Task<ApiEnvelope<GetHolidaysResponse>> GetHolidaysAsync(
        [Query] string from,
        [Query] string to,
        CancellationToken cancellationToken = default);
}
