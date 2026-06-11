namespace GdeOni.Mobile.Shared.Relationships;

/// <summary>
/// Значения relationship type, синхронизированные с backend enum
/// UserRole.RelationshipType (D11.1.1 — JsonStringEnumConverter,
/// строковые имена в API).
/// </summary>
public static class RelationshipTypeValues
{
    public const string Parent = "Parent";
    public const string Grandparent = "Grandparent";
    public const string Child = "Child";
    public const string Spouse = "Spouse";
    public const string Sibling = "Sibling";
    public const string Relative = "Relative";
    public const string Friend = "Friend";
    public const string Acquaintance = "Acquaintance";
    public const string Other = "Other";
}

public sealed record RelationshipOption(string Value, string Display);

/// <summary>
/// Полный список relationship'ов с человеко-читаемыми названиями плюс
/// Display(value) для отображения произвольного значения с lenient
/// fallback. Используется в формах создания карточки и в карточке
/// умершего ("Кем приходится").
/// </summary>
public static class RelationshipCatalog
{
    public static IReadOnlyList<RelationshipOption> All { get; } = new[]
    {
        new RelationshipOption(RelationshipTypeValues.Parent, "Родитель"),
        new RelationshipOption(RelationshipTypeValues.Grandparent, "Дедушка / бабушка"),
        new RelationshipOption(RelationshipTypeValues.Child, "Ребёнок"),
        new RelationshipOption(RelationshipTypeValues.Spouse, "Супруг(а)"),
        new RelationshipOption(RelationshipTypeValues.Sibling, "Брат / сестра"),
        new RelationshipOption(RelationshipTypeValues.Relative, "Другой родственник"),
        new RelationshipOption(RelationshipTypeValues.Friend, "Друг"),
        new RelationshipOption(RelationshipTypeValues.Acquaintance, "Знакомый"),
        new RelationshipOption(RelationshipTypeValues.Other, "Другое"),
    };

    private static readonly Dictionary<string, string> Lookup =
        All.ToDictionary(o => o.Value, o => o.Display, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Возвращает русское название по строковому значению. Lenient:
    /// если значение не распознано (новый тип в backend, который mobile
    /// ещё не знает) — возвращает его как есть. null/empty → "—".
    /// </summary>
    public static string Display(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "—";
        return Lookup.TryGetValue(value, out var display) ? display : value;
    }
}
