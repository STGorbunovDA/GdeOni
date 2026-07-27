using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Geo;

/// <summary>
/// Прямое геокодирование: текст адреса (город / кладбище) → координаты.
/// Используется формой «добавить умершего», чтобы по введённому городу
/// подставить точку на карте, пока у пользователя ещё нет координат.
///
/// Как и <see cref="IReverseGeocoder"/>, живёт на бэкенде: прямой запрос из
/// браузера отправил бы IP пользователя в Nominatim (ЕС), а Политика
/// конфиденциальности (5.3) это запрещает. Наружу уходит только текст
/// адреса, не персональные данные.
/// </summary>
public interface IForwardGeocoder
{
    /// <summary>
    /// Ищет координаты по текстовому адресу. Failure, если геокодер выключен,
    /// недоступен или ничего не нашёл — это НЕ ошибка сценария: пользователь
    /// поставит точку на карте сам.
    /// </summary>
    Task<Result<ForwardGeocodeResult, Error>> Search(
        string query,
        CancellationToken cancellationToken);
}

/// <summary>Результат прямого геокодирования: координаты найденного места.</summary>
public sealed record ForwardGeocodeResult(
    double Latitude,
    double Longitude,
    string? DisplayName);
