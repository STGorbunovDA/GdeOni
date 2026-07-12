namespace GdeOni.Application.Anniversaries;

/// <summary>
/// D37. Тип годовщины, о которой шлётся email-напоминание. Значения
/// стабильны — попадают в БД (столбец <c>kind</c> лога отправленных
/// писем), менять нельзя.
/// </summary>
public enum AnniversaryKind
{
    /// <summary>Годовщина со дня смерти (день памяти).</summary>
    Death = 1,

    /// <summary>Годовщина со дня рождения.</summary>
    Birth = 2,
}
