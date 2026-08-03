using GdeOni.Domain.Aggregates.Relatives;

namespace GdeOni.Domain.Tests.RelativesAggregate;

/// <summary>
/// Turn-based правила переписки «Родственники»: по одному сообщению по
/// очереди; своё последнее можно править/удалять, пока собеседник не ответил;
/// удаление возвращает ход автору.
/// </summary>
public sealed class RelativeConversationTests
{
    private static readonly Guid Deceased = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

    private static RelativeConversation NewConversation() =>
        RelativeConversation.Create(Deceased, UserA, UserB, Now).Value;

    [Fact]
    public void Create_SelfConversation_Rejected()
    {
        var result = RelativeConversation.Create(Deceased, UserA, UserA, Now);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("relatives.conversation.self");
    }

    [Fact]
    public void EmptyConversation_BothCanInitiate()
    {
        var c = NewConversation();
        c.CanSend(UserA).Should().BeTrue();
        c.CanSend(UserB).Should().BeTrue();
    }

    [Fact]
    public void NonParticipant_CannotSend()
    {
        var c = NewConversation();
        c.CanSend(Guid.NewGuid()).Should().BeFalse();
        c.SendMessage(Guid.NewGuid(), "hi", Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AfterSend_SenderWaits_OtherCanReply()
    {
        var c = NewConversation();
        c.SendMessage(UserA, "hi", Now).IsSuccess.Should().BeTrue();

        c.CanSend(UserA).Should().BeFalse();
        c.SendMessage(UserA, "again", Now).Error.Code.Should().Be("relatives.message.not_your_turn");

        c.CanSend(UserB).Should().BeTrue();
        c.SendMessage(UserB, "hello", Now).IsSuccess.Should().BeTrue();

        // Ход вернулся к A.
        c.CanSend(UserA).Should().BeTrue();
        c.CanSend(UserB).Should().BeFalse();
    }

    [Fact]
    public void Edit_OwnLast_AllowedUntilReply_ThenLocked()
    {
        var c = NewConversation();
        var msg = c.SendMessage(UserA, "hi", Now).Value;

        c.EditMessage(msg.Id, UserA, "hi edited", Now).IsSuccess.Should().BeTrue();

        // Собеседник ответил — теперь замок.
        c.SendMessage(UserB, "reply", Now).IsSuccess.Should().BeTrue();
        c.EditMessage(msg.Id, UserA, "too late", Now).Error.Code.Should().Be("relatives.message.locked");
    }

    [Fact]
    public void Edit_OthersMessage_Forbidden()
    {
        var c = NewConversation();
        var msg = c.SendMessage(UserA, "hi", Now).Value;
        c.EditMessage(msg.Id, UserB, "hack", Now).Error.Code.Should().Be("relatives.message.forbidden");
    }

    [Fact]
    public void Delete_LastOwn_RevertsTurn()
    {
        var c = NewConversation();
        var msg = c.SendMessage(UserA, "hi", Now).Value;

        // До удаления ход у B.
        c.CanSend(UserA).Should().BeFalse();

        c.DeleteMessage(msg.Id, UserA, Now).IsSuccess.Should().BeTrue();

        // После удаления диалог снова «пуст» → A может писать заново.
        c.CanSend(UserA).Should().BeTrue();
        c.Messages.Single().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_AfterReply_Locked()
    {
        var c = NewConversation();
        var msg = c.SendMessage(UserA, "hi", Now).Value;
        c.SendMessage(UserB, "reply", Now);

        c.DeleteMessage(msg.Id, UserA, Now).Error.Code.Should().Be("relatives.message.locked");
    }

    [Fact]
    public void Send_EmptyText_Rejected()
    {
        var c = NewConversation();
        c.SendMessage(UserA, "   ", Now).Error.Code.Should().Be("relatives.message.text.required");
    }

    [Fact]
    public void MarkReadBy_MarksOthersMessages()
    {
        var c = NewConversation();
        c.SendMessage(UserA, "hi", Now);

        c.MarkReadBy(UserB, Now);
        c.Messages.Single().IsRead.Should().BeTrue();

        // Свои сообщения себе не помечаем «прочитанными».
        var c2 = NewConversation();
        c2.SendMessage(UserA, "hi", Now);
        c2.MarkReadBy(UserA, Now);
        c2.Messages.Single().IsRead.Should().BeFalse();
    }
}
