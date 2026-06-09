using System.Globalization;
using GdeOni.Mobile.Services.Api.Models;

namespace GdeOni.Mobile.ViewModels.Support;

/// <summary>
/// D25.2. View-model одного сообщения в чате тикета.
/// </summary>
public sealed class ChatMessage
{
    public string AuthorKind { get; init; } = "User";
    public string AuthorLabel { get; init; } = "";
    public string Text { get; init; } = "";
    public string CreatedAt { get; init; } = "";
    public string BackgroundColor { get; init; } = "#FFFFFF";
    public string LabelColor { get; init; } = "#000000";
    public LayoutOptions HorizontalOptions { get; init; } = LayoutOptions.Start;

    /// <summary>
    /// Создаёт ChatMessage с точки зрения юзера-зрителя: свои —
    /// справа, жёлтые; админа — слева, белые.
    /// </summary>
    public static ChatMessage ForViewer(SupportTicketMessageDto dto, bool viewerIsAdmin)
    {
        var isMyMessage = viewerIsAdmin
            ? dto.AuthorKind == "Admin"
            : dto.AuthorKind == "User";

        var authorLabel = dto.AuthorKind switch
        {
            "Admin" => "Администратор",
            "User" => "Пользователь",
            _ => dto.AuthorKind,
        };

        // Свои сообщения — синие и справа, чужие — жёлтые и слева.
        // Делаю наоборот специально: лучше чтобы "сообщение от админа"
        // у юзера было ВИДНО (жёлтое), а свои сливались с фоном (белые).
        // У админа — наоборот: своё (Admin) сливается, юзера (User)
        // подсвечивается.
        return new ChatMessage
        {
            AuthorKind = dto.AuthorKind,
            AuthorLabel = authorLabel,
            Text = dto.Text,
            CreatedAt = dto.CreatedAtUtc.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            BackgroundColor = isMyMessage ? "#FFFFFF" : "#FFF8E1",
            LabelColor = isMyMessage ? "#1976D2" : "#B26A00",
            HorizontalOptions = isMyMessage ? LayoutOptions.End : LayoutOptions.Start,
        };
    }
}
