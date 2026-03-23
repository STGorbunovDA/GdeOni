namespace GdeOni.Domain.Shared;

public enum RelationshipType
{
    /// <summary>
    /// Родитель
    /// </summary>
    Parent = 1,
    /// <summary>
    /// Дедушка/бабушка
    /// </summary>
    Grandparent = 2,
    /// <summary>
    /// Ребенок
    /// </summary>
    Child = 3,
    /// <summary>
    /// Супруг(а)
    /// </summary>
    Spouse = 4,
    /// <summary>
    /// Брат/сестра
    /// </summary>
    Sibling = 5,
    /// <summary>
    /// Другой родственник
    /// </summary>
    Relative = 6,
    /// <summary>
    /// Друг
    /// </summary>
    Friend = 7,
    /// <summary>
    /// Знакомый
    /// </summary>
    Acquaintance = 8,
    Other = 99
}