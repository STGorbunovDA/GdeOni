namespace GdeOni.Mobile.Shared.Routing;

/// <summary>
/// Географическая точка (lat/lon в degrees) для билдеров deep-link
/// URL и для оптимизации порядка обхода. Намеренно отделена от
/// Microsoft.Maui.Devices.Sensors.Location, чтобы Shared-сборка
/// не тащила MAUI в test-проект.
/// </summary>
public sealed record RoutePoint(double Latitude, double Longitude);
