namespace GdeOni.API.Models.Relatives;

/// <summary>Тело POST /api/relatives/conversations/{id}/messages.</summary>
public sealed class SendMessageRequest
{
    /// <summary>Текст сообщения.</summary>
    public string Text { get; set; } = null!;
}

/// <summary>Тело PATCH /api/relatives/conversations/{id}/messages/{messageId}.</summary>
public sealed class EditMessageRequest
{
    /// <summary>Новый текст сообщения.</summary>
    public string Text { get; set; } = null!;
}
