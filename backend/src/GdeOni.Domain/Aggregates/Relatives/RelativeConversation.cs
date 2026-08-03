using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Relatives;

/// <summary>
/// Диалог двух пользователей-родственников в контексте одной карточки
/// умершего. Переписка строго ПО ОЧЕРЕДИ, по одному сообщению: отправить
/// можно, только если последнее видимое сообщение — не твоё (собеседник
/// ответил); своё последнее сообщение можно править/удалять, пока собеседник
/// не ответил. Участники хранятся канонично (A &lt; B) — один диалог на пару
/// в контексте карточки.
/// </summary>
public sealed class RelativeConversation : Entity<Guid>
{
    public Guid DeceasedId { get; }
    public Guid ParticipantAId { get; }
    public Guid ParticipantBId { get; }
    public DateTime CreatedAtUtc { get; }

    /// <summary>Время последней активности — для сортировки списка диалогов.</summary>
    public DateTime LastMessageAtUtc { get; private set; }

    private readonly List<RelativeMessage> _messages = new();
    public IReadOnlyCollection<RelativeMessage> Messages => _messages.AsReadOnly();

    private RelativeConversation() : base(Guid.Empty) { }

    private RelativeConversation(Guid id, Guid deceasedId, Guid a, Guid b, DateTime nowUtc)
        : base(id)
    {
        DeceasedId = deceasedId;
        ParticipantAId = a;
        ParticipantBId = b;
        CreatedAtUtc = nowUtc;
        LastMessageAtUtc = nowUtc;
    }

    public static Result<RelativeConversation, Error> Create(
        Guid deceasedId, Guid userX, Guid userY, DateTime nowUtc)
    {
        if (userX == userY)
            return Errors.Relatives.CannotMessageSelf();

        var (a, b) = Canonical(userX, userY);
        return Result.Success<RelativeConversation, Error>(
            new RelativeConversation(Guid.NewGuid(), deceasedId, a, b, nowUtc));
    }

    public bool IsParticipant(Guid userId) =>
        userId == ParticipantAId || userId == ParticipantBId;

    public Guid OtherParticipant(Guid userId) =>
        userId == ParticipantAId ? ParticipantBId : ParticipantAId;

    /// <summary>Последнее НЕ удалённое сообщение (по времени создания).</summary>
    public RelativeMessage? LastVisibleMessage() =>
        _messages.Where(m => !m.IsDeleted)
            .OrderBy(m => m.CreatedAtUtc)
            .LastOrDefault();

    /// <summary>Ход пользователя: диалог пуст ИЛИ последнее видимое сообщение — чужое.</summary>
    public bool CanSend(Guid userId)
    {
        if (!IsParticipant(userId))
            return false;

        var last = LastVisibleMessage();
        return last is null || last.SenderId != userId;
    }

    public Result<RelativeMessage, Error> SendMessage(Guid senderId, string text, DateTime nowUtc)
    {
        if (!IsParticipant(senderId))
            return Errors.Relatives.NotParticipant();

        if (!CanSend(senderId))
            return Errors.Relatives.NotYourTurn();

        var msgResult = RelativeMessage.Create(Id, senderId, text, nowUtc);
        if (msgResult.IsFailure)
            return msgResult.Error;

        _messages.Add(msgResult.Value);
        LastMessageAtUtc = nowUtc;
        return Result.Success<RelativeMessage, Error>(msgResult.Value);
    }

    public UnitResult<Error> EditMessage(Guid messageId, Guid editorId, string text, DateTime nowUtc)
    {
        if (!IsParticipant(editorId))
            return Errors.Relatives.NotParticipant();

        var msg = _messages.FirstOrDefault(m => m.Id == messageId);
        if (msg is null || msg.IsDeleted)
            return Errors.Relatives.MessageNotFound();

        if (msg.SenderId != editorId)
            return Errors.Relatives.NotOwnMessage();

        // Правка только пока это последнее видимое (собеседник ещё не ответил).
        if (LastVisibleMessage()?.Id != messageId)
            return Errors.Relatives.MessageLocked();

        return msg.Edit(text, nowUtc);
    }

    public UnitResult<Error> DeleteMessage(Guid messageId, Guid deleterId, DateTime nowUtc)
    {
        if (!IsParticipant(deleterId))
            return Errors.Relatives.NotParticipant();

        var msg = _messages.FirstOrDefault(m => m.Id == messageId);
        if (msg is null || msg.IsDeleted)
            return Errors.Relatives.MessageNotFound();

        if (msg.SenderId != deleterId)
            return Errors.Relatives.NotOwnMessage();

        if (LastVisibleMessage()?.Id != messageId)
            return Errors.Relatives.MessageLocked();

        msg.Delete(nowUtc);
        // Ход возвращается автору (последнее видимое теперь — предыдущее чужое
        // или пусто) — это и значит «забрал сообщение».
        return UnitResult.Success<Error>();
    }

    /// <summary>Помечает прочитанными сообщения ОТ собеседника — readerId их открыл.</summary>
    public void MarkReadBy(Guid readerId, DateTime nowUtc)
    {
        if (!IsParticipant(readerId))
            return;

        foreach (var m in _messages.Where(m => !m.IsDeleted && m.SenderId != readerId && !m.IsRead))
            m.MarkRead(nowUtc);
    }

    private static (Guid A, Guid B) Canonical(Guid x, Guid y) =>
        x.CompareTo(y) <= 0 ? (x, y) : (y, x);
}
