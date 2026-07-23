using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Support;

/// <summary>
/// Запрос создания обращения в поддержку от пользователя.
/// </summary>
public sealed class CreateSupportTicketRequest
{
    /// <summary>Тип обращения (баг, правка карточки, общий вопрос и т.д.).</summary>
    public SupportTicketKind Kind { get; set; }
    /// <summary>Заголовок обращения.</summary>
    public string Title { get; set; } = null!;
    /// <summary>Текст обращения.</summary>
    public string Description { get; set; } = null!;
}
