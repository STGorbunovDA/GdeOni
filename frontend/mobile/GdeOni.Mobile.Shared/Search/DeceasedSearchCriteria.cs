namespace GdeOni.Mobile.Shared.Search;

/// <summary>
/// Правила "можно ли запускать поиск" для формы DeceasedSearch (E16 /
/// E17.4.1 mobile, F6 web). Поиск без критериев перебирает всю базу —
/// не пускаем. Критерий валиден, если задано ХОТЯ БЫ ОДНО из:
/// - текстовое поле (Query / FirstName / LastName / MiddleName) длиной
///   >= MinTextFieldLength после Trim;
/// - включён фильтр по дате рождения;
/// - включён фильтр по дате смерти.
/// Город сам по себе НЕ является валидным критерием (по нему одному
/// поиск перебрал бы город целиком), но дополняет другие.
/// </summary>
public static class DeceasedSearchCriteria
{
    public const int MinTextFieldLength = 2;

    public static bool IsTextFieldLongEnough(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Trim().Length >= MinTextFieldLength;

    public static bool CanSearch(
        string? query,
        string? firstName,
        string? lastName,
        string? middleName,
        bool useBirthDateFilter,
        bool useDeathDateFilter) =>
        IsTextFieldLongEnough(query)
        || IsTextFieldLongEnough(firstName)
        || IsTextFieldLongEnough(lastName)
        || IsTextFieldLongEnough(middleName)
        || useBirthDateFilter
        || useDeathDateFilter;
}
