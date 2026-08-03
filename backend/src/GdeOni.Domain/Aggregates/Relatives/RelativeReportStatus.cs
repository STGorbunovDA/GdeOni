namespace GdeOni.Domain.Aggregates.Relatives;

/// <summary>
/// Состояние жалобы на родственника (Фаза 5). Хранится как int
/// (HasConversion&lt;int&gt;) — добавление значений не требует миграции.
/// </summary>
public enum RelativeReportStatus
{
    /// <summary>Новая жалоба, ждёт разбора админом.</summary>
    Pending = 0,

    /// <summary>Разобрана админом (с пометкой о решении).</summary>
    Resolved = 1,
}
