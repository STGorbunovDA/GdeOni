namespace GdeOni.Domain.Shared;

public static partial class Errors
{
    /// <summary>
    /// D41. Обратное геокодирование (координаты → город).
    ///
    /// Обе ошибки — НЕ провал сценария: определение города это подсказка,
    /// а не обязательный шаг. Клиент, получив их, просто оставляет поля
    /// пустыми, и юзер вписывает город сам.
    /// </summary>
    public static class Geo
    {
        /// <summary>Геокодер выключен, не ответил или упал.</summary>
        public static Error GeocodingUnavailable() =>
            Error.Failure(
                "geo.geocoding.unavailable",
                "Geocoding service is unavailable.");

        /// <summary>Координаты валидны, но адреса там нет (море, тайга).</summary>
        public static Error AddressNotFound() =>
            Error.NotFound(
                "geo.address.not_found",
                "No address found for the given coordinates.");
    }
}
