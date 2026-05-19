using System.Globalization;

namespace GdeOni.Mobile.Shared.Utils;

/// <summary>
/// Парсер строковых значений широты / долготы / точности (метры) в double.
/// Принимает и точку, и запятую как десятичный разделитель (на ru-RU
/// клавиатуре по умолчанию запятая). Парсит всегда через InvariantCulture,
/// чтобы поведение не зависело от системной локали.
/// </summary>
public static class CoordinateParser
{
    /// <summary>
    /// Парсит произвольный double. Trim'ит ввод и заменяет ',' на '.'.
    /// Возвращает false, если ввод не парсится.
    /// </summary>
    public static bool TryParseDouble(string? input, out double value)
    {
        var normalized = (input ?? string.Empty).Trim().Replace(',', '.');
        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    /// <summary>
    /// Парсит широту с проверкой диапазона [-90, 90].
    /// </summary>
    public static bool TryParseLatitude(string? input, out double value)
    {
        if (!TryParseDouble(input, out value)) return false;
        return value is >= -90 and <= 90;
    }

    /// <summary>
    /// Парсит долготу с проверкой диапазона [-180, 180].
    /// </summary>
    public static bool TryParseLongitude(string? input, out double value)
    {
        if (!TryParseDouble(input, out value)) return false;
        return value is >= -180 and <= 180;
    }

    /// <summary>
    /// Парсит точность в метрах — должно быть неотрицательным числом.
    /// </summary>
    public static bool TryParseAccuracy(string? input, out double value)
    {
        if (!TryParseDouble(input, out value)) return false;
        return value >= 0;
    }
}
