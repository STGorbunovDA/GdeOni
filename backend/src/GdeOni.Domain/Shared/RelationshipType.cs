namespace GdeOni.Domain.Shared;

public enum RelationshipType
{
    /// <summary>
    /// Родитель (устаревшее, объединённое — только для СТАРЫХ записей;
    /// в новых карточках используются раздельные Mother / Father).
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
    /// Брат/сестра (устаревшее, объединённое — только для СТАРЫХ записей;
    /// в новых карточках используются раздельные Brother / Sister).
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
    /// <summary>
    /// Мама
    /// </summary>
    Mother = 14,
    /// <summary>
    /// Папа
    /// </summary>
    Father = 15,
    /// <summary>
    /// Брат
    /// </summary>
    Brother = 16,
    /// <summary>
    /// Сестра
    /// </summary>
    Sister = 17,
    Other = 99
}