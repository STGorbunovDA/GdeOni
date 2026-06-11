using Microsoft.Maui.Devices.Sensors;

namespace GdeOni.Mobile.Services.Geolocation;

public sealed class GeolocationService : IGeolocationService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    public async Task<GeolocationResult> RequestAndGetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await EnsurePermissionAsync();
            if (status.ErrorKind is not null)
                return status;

            var location = await Microsoft.Maui.Devices.Sensors.Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, RequestTimeout),
                cancellationToken);

            if (location is null)
            {
                return new GeolocationResult(
                    Success: false,
                    Latitude: null,
                    Longitude: null,
                    AccuracyMeters: null,
                    ErrorKind: GeolocationErrorKind.Unknown,
                    ErrorMessage: "Не удалось определить местоположение.");
            }

            return new GeolocationResult(
                Success: true,
                Latitude: location.Latitude,
                Longitude: location.Longitude,
                AccuracyMeters: location.Accuracy,
                ErrorKind: null,
                ErrorMessage: null);
        }
        catch (FeatureNotSupportedException)
        {
            return new GeolocationResult(false, null, null, null,
                GeolocationErrorKind.LocationServicesDisabled,
                "Геолокация не поддерживается на этом устройстве.");
        }
        catch (FeatureNotEnabledException)
        {
            return new GeolocationResult(false, null, null, null,
                GeolocationErrorKind.LocationServicesDisabled,
                "Включите геолокацию в настройках устройства.");
        }
        catch (PermissionException)
        {
            return new GeolocationResult(false, null, null, null,
                GeolocationErrorKind.PermissionDeniedPermanent,
                "Доступ к геолокации запрещён. Откройте настройки приложения и разрешите доступ.");
        }
        catch (TaskCanceledException)
        {
            return new GeolocationResult(false, null, null, null,
                GeolocationErrorKind.Timeout,
                "Не удалось получить координаты — таймаут. Попробуйте на открытом небе.");
        }
        catch (Exception ex)
        {
            return new GeolocationResult(false, null, null, null,
                GeolocationErrorKind.Unknown,
                $"Ошибка геолокации: {ex.Message}");
        }
    }

    public void OpenAppSettings()
    {
        AppInfo.Current.ShowSettingsUI();
    }

    private static async Task<GeolocationResult> EnsurePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
            return new GeolocationResult(true, null, null, null, null, null);

        // Если ранее юзер уже отклонил и поставил "не спрашивать снова" —
        // ShouldShowRationale возвращает false на Android. В этом случае
        // повторный Request() ничего не покажет, и нужно слать в настройки.
        var shouldShowRationale = Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>();

        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (status == PermissionStatus.Granted)
            return new GeolocationResult(true, null, null, null, null, null);

        if (!shouldShowRationale && status != PermissionStatus.Granted)
        {
            return new GeolocationResult(
                Success: false,
                Latitude: null,
                Longitude: null,
                AccuracyMeters: null,
                ErrorKind: GeolocationErrorKind.PermissionDeniedPermanent,
                ErrorMessage: "Доступ запрещён навсегда. Откройте настройки приложения.");
        }

        return new GeolocationResult(
            Success: false,
            Latitude: null,
            Longitude: null,
            AccuracyMeters: null,
            ErrorKind: GeolocationErrorKind.PermissionDenied,
            ErrorMessage: "Доступ к геолокации не разрешён.");
    }
}
