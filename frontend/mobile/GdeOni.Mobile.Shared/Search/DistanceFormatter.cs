using System.Globalization;

namespace GdeOni.Mobile.Shared.Search;

/// <summary>
/// Форматирование расстояния "до могилы" для UI (E21 nearby search).
/// Backend возвращает целое число метров; на UI показываем "120 м"
/// до 1 км и "1.2 км" дальше — стандартный паттерн карт-приложений.
/// Используется invariant culture, чтобы десятичный разделитель
/// не зависел от системной локали (всегда точка).
/// </summary>
public static class DistanceFormatter
{
    public const int MetersToKilometersThreshold = 1000;

    public static string Format(int meters)
    {
        if (meters < 0) meters = 0;

        if (meters < MetersToKilometersThreshold)
            return $"{meters} м";

        var km = meters / 1000.0;
        return string.Format(CultureInfo.InvariantCulture, "{0:0.#} км", km);
    }
}
