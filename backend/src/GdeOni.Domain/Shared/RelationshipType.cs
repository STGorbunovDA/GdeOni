namespace GdeOni.Domain.Shared;

public enum RelationshipType
{
    /// <summary>
    /// Родитель
    /// </summary>
    Parent = 1,
    /// <summary>
    /// Дедушка/бабушка (устаревшее, объединённое — только для СТАРЫХ записей;
    /// в новых карточках используются раздельные Grandfather / Grandmother.
    /// Не удалять: в БД хранится как число 2, снос сломает старые строки).
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
    /// <summary>
    /// Прадедушка (прадед)
    /// </summary>
    GreatGrandfather = 9,
    /// <summary>
    /// Прабабушка (прабабка)
    /// </summary>
    GreatGrandmother = 10,
    /// <summary>
    /// Дальний родственник
    /// </summary>
    DistantRelative = 11,
    /// <summary>
    /// Дедушка
    /// </summary>
    Grandfather = 12,
    /// <summary>
    /// Бабушка
    /// </summary>
    Grandmother = 13,
    Other = 99
}