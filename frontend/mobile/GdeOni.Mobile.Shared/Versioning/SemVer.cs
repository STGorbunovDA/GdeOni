namespace GdeOni.Mobile.Shared.Versioning;

/// <summary>
/// E22. Минимальный SemVer-парсер для проверки version-gate
/// (<c>currentVersion >= minSupportedVersion</c> и т.п.).
///
/// Поддерживает только формат <c>"Major.Minor.Patch"</c> с целочисленными
/// компонентами без префиксов/суффиксов (без <c>-alpha</c>, <c>+build</c>
/// и т.д.). Это сознательное ограничение: бэк-приложение раздаётся
/// одним числом, нам не нужны pre-release ветки сейчас.
/// </summary>
public readonly record struct SemVer(int Major, int Minor, int Patch) : IComparable<SemVer>
{
    /// <summary>
    /// Парсит строку вида "1.2.3". Возвращает true и заполняет
    /// <paramref name="version"/> при успехе. На некорректном
    /// формате — false (например, "1.2", "1.2.3-rc", "v1.2.3",
    /// отрицательные числа, пустая строка).
    /// </summary>
    public static bool TryParse(string? input, out SemVer version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var parts = input.Split('.');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out var major) || major < 0)
            return false;
        if (!int.TryParse(parts[1], out var minor) || minor < 0)
            return false;
        if (!int.TryParse(parts[2], out var patch) || patch < 0)
            return false;

        version = new SemVer(major, minor, patch);
        return true;
    }

    /// <summary>
    /// Парсит строку вида "1.2.3". На некорректном формате бросает
    /// <see cref="FormatException"/>. Используется когда невалидный
    /// version-string — это программная ошибка (например, в тестах
    /// или при чтении version из манифеста приложения).
    /// </summary>
    public static SemVer Parse(string input)
    {
        if (!TryParse(input, out var version))
            throw new FormatException($"'{input}' не является корректной SemVer-строкой 'Major.Minor.Patch'.");
        return version;
    }

    /// <summary>
    /// true если <c>this >= other</c> по compound-сравнению Major→Minor→Patch.
    /// Используется для проверки "клиент удовлетворяет min-supported-version".
    /// </summary>
    public bool IsAtLeast(SemVer other) => CompareTo(other) >= 0;

    public int CompareTo(SemVer other)
    {
        var majorCmp = Major.CompareTo(other.Major);
        if (majorCmp != 0) return majorCmp;

        var minorCmp = Minor.CompareTo(other.Minor);
        if (minorCmp != 0) return minorCmp;

        return Patch.CompareTo(other.Patch);
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    public static bool operator <(SemVer left, SemVer right) => left.CompareTo(right) < 0;
    public static bool operator >(SemVer left, SemVer right) => left.CompareTo(right) > 0;
    public static bool operator <=(SemVer left, SemVer right) => left.CompareTo(right) <= 0;
    public static bool operator >=(SemVer left, SemVer right) => left.CompareTo(right) >= 0;
}
