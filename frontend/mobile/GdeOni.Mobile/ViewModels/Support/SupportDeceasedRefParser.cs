using System.Text.RegularExpressions;

namespace GdeOni.Mobile.ViewModels.Support;

/// <summary>
/// D34. Распознаёт ссылку на карточку умершего внутри Description
/// тикета. Шаблон проставляется автоматически в SupportNewViewModel
/// при переходе с DeceasedDetailsPage — маркер "ID карточки: {guid}".
/// Если юзер пришёл из карточки и не удалил эту строку — админ
/// получит кнопку быстрого перехода.
/// </summary>
internal static class SupportDeceasedRefParser
{
    // \b — границы слова; group "id" — стандартный Guid 8-4-4-4-12.
    private static readonly Regex GuidRegex = new(
        @"ID карточки:\s*(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled);

    public static Guid? TryExtract(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var match = GuidRegex.Match(description);
        if (!match.Success) return null;
        return Guid.TryParse(match.Groups["id"].Value, out var id) ? id : null;
    }
}
