namespace GdeOni.Domain.Shared;

/// <summary>
/// Функция «Родственники»: какие типы связи показываются как «родственник» в
/// списке и учитываются при матчинге. Скрываем «Знакомый» (Acquaintance) и
/// «Другое» (Other) — они не подразумевают родства/близости. Всё остальное
/// (семья, друг, «Родственник», включая старые объединённые Parent /
/// Grandparent / Sibling) считается связывающим.
/// </summary>
public static class RelativeRelationships
{
    /// <summary>Связи, попадающие в «Родственники».</summary>
    public static readonly IReadOnlySet<RelationshipType> Connectable =
        Enum.GetValues<RelationshipType>()
            .Where(r => r is not RelationshipType.Acquaintance
                          and not RelationshipType.Other)
            .ToHashSet();

    public static bool IsConnectable(RelationshipType type) => Connectable.Contains(type);
}
