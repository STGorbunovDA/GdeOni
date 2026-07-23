namespace GdeOni.Application.Common.Shared;

/// <summary>
/// Русская плюрализация числительных (1 год / 2 года / 5 лет). Нужна
/// для человекочитаемых сообщений (D37: тело email о годовщинах).
/// </summary>
public static class RussianPlural
{
    /// <summary>
    /// Возвращает согласованное с числом слово для «года»:
    /// 1 год, 2/3/4 года, 5..20 лет, 21 год, 22 года и т.д.
    /// </summary>
    public static string Years(int count) => Pick(count, "год", "года", "лет");

    /// <summary>
    /// Универсальный выбор формы по правилам русского языка.
    /// <paramref name="one"/> — для «1, 21, 31...»;
    /// <paramref name="few"/> — для «2-4, 22-24...»;
    /// <paramref name="many"/> — для «0, 5-20, 11-14...».
    /// </summary>
    public static string Pick(int count, string one, string few, string many)
    {
        var n = Math.Abs(count);
        var mod100 = n % 100;
        var mod10 = n % 10;

        if (mod100 is >= 11 and <= 14)
            return many;

        return mod10 switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many,
        };
    }
}
