using System.Text.Json;
using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

/// <summary>
/// D24. Запись аудита одной правки карточки умершего. Создаётся только
/// внутри агрегата <see cref="Deceased"/>: ни Application, ни Infrastructure
/// не могут сфабриковать пустой/левый edit — гарантирует целостность лога.
///
/// ChangesJson — словарь { "FieldName": { "old": "...", "new": "..." } }
/// сериализованный в строку. Используем json вместо отдельных колонок
/// потому что набор полей в одном Kind'е variable (например, в MainInfo
/// — 7 полей, в Metadata — 5, в BurialLocation — 9), и админ-таблицам
/// удобнее общий формат.
/// </summary>
public sealed class DeceasedEdit : Entity<Guid>
{
    public const int MaxChangesJsonLength = 8 * 1024;

    public Guid DeceasedId { get; private set; }
    public Guid? EditedByUserId { get; private set; }
    public DateTime EditedAtUtc { get; private set; }
    public DeceasedEditKind Kind { get; private set; }
    public string ChangesJson { get; private set; } = null!;

    private DeceasedEdit() : base(Guid.Empty) { }

    private DeceasedEdit(
        Guid id,
        Guid deceasedId,
        Guid editedByUserId,
        DateTime editedAtUtc,
        DeceasedEditKind kind,
        string changesJson) : base(id)
    {
        DeceasedId = deceasedId;
        EditedByUserId = editedByUserId;
        EditedAtUtc = editedAtUtc;
        Kind = kind;
        ChangesJson = changesJson;
    }

    /// <summary>
    /// Фабрика: принимает уже посчитанный diff и сериализует его.
    /// Domain не зависит от Application, поэтому diff собирает сам
    /// <see cref="Deceased"/>.
    /// </summary>
    internal static Result<DeceasedEdit, Error> Create(
        Guid deceasedId,
        Guid editedByUserId,
        DeceasedEditKind kind,
        IReadOnlyDictionary<string, ChangePair> changes)
    {
        if (deceasedId == Guid.Empty)
            return Errors.DeceasedEdit.DeceasedIdRequired();
        if (editedByUserId == Guid.Empty)
            return Errors.DeceasedEdit.EditorIdRequired();
        if (changes.Count == 0)
            return Errors.DeceasedEdit.NoChanges();

        var json = JsonSerializer.Serialize(changes);
        if (json.Length > MaxChangesJsonLength)
            return Errors.DeceasedEdit.ChangesJsonTooLarge(MaxChangesJsonLength);

        return Result.Success<DeceasedEdit, Error>(
            new DeceasedEdit(
                Guid.NewGuid(),
                deceasedId,
                editedByUserId,
                DateTime.UtcNow,
                kind,
                json));
    }
}

/// <summary>
/// Старое/новое значение поля для diff'а. Оба значения — строки, чтобы
/// аудит-API мог отдать униформный формат вне зависимости от типа
/// исходного поля (даты как ISO, bool как "true"/"false", null как null).
/// </summary>
public sealed record ChangePair(string? Old, string? New);
