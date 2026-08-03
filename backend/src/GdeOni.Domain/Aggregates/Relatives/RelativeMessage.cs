using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Relatives;

/// <summary>
/// Сообщение во внутренней переписке «Родственники». Дочерняя сущность
/// <see cref="RelativeConversation"/> — создаётся/меняется только через методы
/// диалога (turn-based правила там). Удаление мягкое (soft-delete): текст
/// стирается, строка остаётся для восстановления хода.
/// </summary>
public sealed class RelativeMessage : Entity<Guid>
{
    public const int MaxTextLength = 2000;

    public Guid ConversationId { get; }
    public Guid SenderId { get; }
    public string Text { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? EditedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    /// <summary>Прочитано ли сообщение получателем (для «непрочитанных» и уведомлений).</summary>
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    private RelativeMessage() : base(Guid.Empty)
    {
        Text = null!;
    }

    private RelativeMessage(Guid id, Guid conversationId, Guid senderId, string text, DateTime createdAtUtc)
        : base(id)
    {
        ConversationId = conversationId;
        SenderId = senderId;
        Text = text;
        CreatedAtUtc = createdAtUtc;
    }

    internal static Result<RelativeMessage, Error> Create(
        Guid conversationId, Guid senderId, string text, DateTime nowUtc)
    {
        var normalized = NormalizeText(text);
        if (normalized.IsFailure)
            return normalized.Error;

        return Result.Success<RelativeMessage, Error>(
            new RelativeMessage(Guid.NewGuid(), conversationId, senderId, normalized.Value, nowUtc));
    }

    internal UnitResult<Error> Edit(string text, DateTime nowUtc)
    {
        var normalized = NormalizeText(text);
        if (normalized.IsFailure)
            return normalized.Error;

        if (Text == normalized.Value)
            return UnitResult.Success<Error>();

        Text = normalized.Value;
        EditedAtUtc = nowUtc;
        return UnitResult.Success<Error>();
    }

    internal void Delete(DateTime nowUtc)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAtUtc = nowUtc;
        Text = string.Empty; // текст удалённого не храним
    }

    internal void MarkRead(DateTime nowUtc)
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAtUtc = nowUtc;
    }

    private static Result<string, Error> NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Errors.Relatives.MessageTextRequired();

        var normalized = text.Trim();
        if (normalized.Length > MaxTextLength)
            return Errors.Relatives.MessageTooLong(MaxTextLength);

        return Result.Success<string, Error>(normalized);
    }
}
