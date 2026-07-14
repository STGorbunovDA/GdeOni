using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

/// <summary>
/// D41. Обратное геокодирование: координаты → страна / город.
///
/// Ходим в НАШ бэкенд, а не напрямую в Nominatim: прямой запрос с телефона
/// отправил бы IP пользователя во внешний сервис в ЕС, а Политика
/// конфиденциальности (5.3) обещает отсутствие трансграничной передачи ПД.
/// </summary>
public interface IGeoApi
{
    /// <summary>
    /// GET /api/geo/reverse. 404 — адреса по точке нет (лес, море),
    /// 500 — геокодер недоступен. Оба случая штатные: вызывающий молча
    /// оставляет поля пустыми, юзер вписывает город сам.
    /// </summary>
    [Get("/api/geo/reverse")]
    Task<ApiEnvelope<ReverseGeocodeResponse>> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
