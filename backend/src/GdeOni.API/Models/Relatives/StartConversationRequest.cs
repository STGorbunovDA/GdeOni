namespace GdeOni.API.Models.Relatives;

/// <summary>Тело POST /api/relatives/conversations — открыть/получить диалог.</summary>
public sealed class StartConversationRequest
{
    /// <summary>Карточка умершего, по которой найден родственник.</summary>
    public Guid DeceasedId { get; set; }

    /// <summary>Пользователь-родственник, которому пишем.</summary>
    public Guid OtherUserId { get; set; }
}
