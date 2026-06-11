using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class DeceasedMemoryEntry : Entity<Guid>
{
    public const int MaxTextLength = 5000;

    public string Text { get; private set; }
    public Guid? AuthorUserId { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public ModerationStatus ModerationStatus { get; private set; }

    private DeceasedMemoryEntry() : base(Guid.Empty)
    {
        Text = null!;
    }

    private DeceasedMemoryEntry(
        Guid id,
        string text,
        Guid? authorUserId,
        DateTime createdAtUtc) : base(id)
    {
        Text = text;
        AuthorUserId = authorUserId;
        CreatedAtUtc = createdAtUtc;
        ModerationStatus = ModerationStatus.Pending;
    }

    public static Result<DeceasedMemoryEntry, Error> Create(
        string text,
        Guid? authorUserId = null)
    {
        if (authorUserId.HasValue && authorUserId.Value == Guid.Empty)
            return Errors.User.IdRequired();

        var textResult = NormalizeAndValidateText(text);
        if (textResult.IsFailure)
            return textResult.Error;

        return Result.Success<DeceasedMemoryEntry, Error>(
            new DeceasedMemoryEntry(
                Guid.NewGuid(),
                textResult.Value,
                authorUserId,
                DateTime.UtcNow));
    }

    public UnitResult<Error> EditText(string text)
    {
        var textResult = NormalizeAndValidateText(text);
        if (textResult.IsFailure)
            return textResult.Error;

        // No-op guard (D11.9.2): автор открыл UI и нажал Save без правок —
        // Approved-запись не должна терять статус модерации. Сравниваем
        // нормализованные формы.
        if (Text == textResult.Value)
            return UnitResult.Success<Error>();

        Text = textResult.Value;
        // Любая фактическая правка текста возвращает запись в Pending.
        // Без этого обходится модерация: автор пишет одобряемое содержимое,
        // ждёт Approved, затем редактирует на произвольный текст —
        // и запись остаётся Approved. Reset при каждом EditText
        // заставляет каждый текст пройти модерацию заново.
        ModerationStatus = ModerationStatus.Pending;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Approve()
    {
        if (ModerationStatus == ModerationStatus.Approved)
            return Errors.DeceasedMemory.AlreadyApproved();

        ModerationStatus = ModerationStatus.Approved;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject()
    {
        if (ModerationStatus == ModerationStatus.Rejected)
            return Errors.DeceasedMemory.AlreadyRejected();

        ModerationStatus = ModerationStatus.Rejected;
        Touch();
        return UnitResult.Success<Error>();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static Result<string, Error> NormalizeAndValidateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Errors.DeceasedMemory.TextRequired();

        var normalized = text.Trim();

        if (normalized.Length > MaxTextLength)
            return Errors.DeceasedMemory.TextTooLong(MaxTextLength);

        return Result.Success<string, Error>(normalized);
    }
}