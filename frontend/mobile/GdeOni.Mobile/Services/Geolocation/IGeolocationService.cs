namespace GdeOni.Mobile.Services.Geolocation;

public interface IGeolocationService
{
    /// <summary>
    /// Запрашивает разрешение на геолокацию (если ещё не выдано) и возвращает
    /// текущие координаты с точностью. Никогда не бросает исключения —
    /// все ошибки оборачиваются в <see cref="GeolocationResult"/>.
    /// </summary>
    Task<GeolocationResult> RequestAndGetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Открывает системные настройки приложения, чтобы пользователь мог
    /// вручную включить разрешение, которое ранее было отклонено навсегда.
    /// </summary>
    void OpenAppSettings();
}

/// <summary>
/// Результат запроса координат. AccuracyMeters — радиус ошибки в метрах,
/// может быть null если устройство не сообщило точность.
/// </summary>
public sealed record GeolocationResult(
    bool Success,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    GeolocationErrorKind? ErrorKind,
    string? ErrorMessage);

public enum GeolocationErrorKind
{
    PermissionDenied,
    PermissionDeniedPermanent,
    LocationServicesDisabled,
    Timeout,
    Unknown
}
