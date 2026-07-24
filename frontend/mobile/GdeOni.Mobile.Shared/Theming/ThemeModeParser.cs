namespace GdeOni.Mobile.Shared.Theming;

/// <summary>
/// E27. Сериализация <see cref="ThemeMode"/> ↔ строка для хранения в
/// Preferences. Строковые значения намеренно совпадают с web
/// (<c>auto</c>/<c>light</c>/<c>dark</c>, ключ <c>gdeoni-color-scheme</c>) —
/// один и тот же словарь на обоих клиентах.
///
/// Логика вынесена в Shared (MAUI-free), чтобы покрыть тестами без
/// Android-workload — сама запись в Preferences и выставление
/// Application.UserAppTheme живут в ThemeService на стороне MAUI.
/// </summary>
public static class ThemeModeParser
{
    public const string StorageKey = "gdeoni-color-scheme";

    private const string Auto = "auto";
    private const string Light = "light";
    private const string Dark = "dark";

    /// <summary>
    /// Разбирает сохранённое значение. Любой мусор/пустое/неизвестное →
    /// <see cref="ThemeMode.System"/> (безопасный дефолт «как в системе»).
    /// Регистр и пробелы игнорируются.
    /// </summary>
    public static ThemeMode Parse(string? stored)
    {
        return stored?.Trim().ToLowerInvariant() switch
        {
            Light => ThemeMode.Light,
            Dark => ThemeMode.Dark,
            _ => ThemeMode.System,
        };
    }

    /// <summary>
    /// Значение для записи в Preferences (совместимо с web).
    /// </summary>
    public static string ToStorageString(ThemeMode mode)
    {
        return mode switch
        {
            ThemeMode.Light => Light,
            ThemeMode.Dark => Dark,
            _ => Auto,
        };
    }
}
