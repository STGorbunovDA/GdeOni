using System.Net;
using System.Text;
using GdeOni.Application.Abstractions.Email;
using GdeOni.Application.Common.Shared;

namespace GdeOni.Application.Anniversaries;

/// <summary>
/// D37. Чистый построитель тела email-напоминания о годовщине. Не
/// зависит ни от SMTP, ни от БД — принимает готовые данные и возвращает
/// <see cref="EmailMessage"/> с текстовой и HTML-версиями. Вынесен
/// отдельно, чтобы покрыть формулировки юнит-тестами без Testcontainers.
/// </summary>
public static class AnniversaryEmailContent
{
    /// <summary>
    /// Собирает письмо для конкретного получателя и годовщины.
    /// </summary>
    /// <param name="recipientEmail">Email подписчика (адресат).</param>
    /// <param name="recipientName">Имя подписчика для приветствия (может быть null).</param>
    /// <param name="kind">Годовщина смерти или рождения.</param>
    /// <param name="deceasedFullName">ФИО умершего для темы и тела.</param>
    /// <param name="yearsSince">Сколько лет исполняется (≥ 1).</param>
    /// <param name="appName">Название сервиса (для подписи).</param>
    /// <param name="appUrl">Ссылка на сайт (опционально, для кнопки/футера).</param>
    public static EmailMessage Build(
        string recipientEmail,
        string? recipientName,
        AnniversaryKind kind,
        string deceasedFullName,
        int yearsSince,
        string appName,
        string? appUrl)
    {
        var yearsWord = RussianPlural.Years(yearsSince);
        var greeting = string.IsNullOrWhiteSpace(recipientName)
            ? "Здравствуйте!"
            : $"Здравствуйте, {recipientName.Trim()}!";

        var (subject, lead) = kind switch
        {
            AnniversaryKind.Death => (
                $"День памяти: {deceasedFullName}",
                $"Сегодня {yearsSince} {yearsWord} со дня смерти {deceasedFullName}."),
            AnniversaryKind.Birth => (
                $"День рождения: {deceasedFullName}",
                $"Сегодня день рождения {deceasedFullName} — исполнилось бы {yearsSince} {yearsWord}."),
            _ => (
                $"Годовщина: {deceasedFullName}",
                $"Сегодня годовщина, связанная с {deceasedFullName}."),
        };

        const string reminder =
            "Хороший повод вспомнить дорогого человека — навестить могилу, " +
            "зажечь свечу или просто помолчать о нём.";

        var text = BuildText(greeting, lead, reminder, appName, appUrl);
        var html = BuildHtml(greeting, lead, reminder, appName, appUrl);

        return new EmailMessage(recipientEmail, recipientName, subject, text, html);
    }

    private static string BuildText(
        string greeting,
        string lead,
        string reminder,
        string appName,
        string? appUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine(greeting);
        sb.AppendLine();
        sb.AppendLine(lead);
        sb.AppendLine();
        sb.AppendLine(reminder);
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(appUrl))
        {
            sb.AppendLine($"Открыть карточку и построить маршрут: {appUrl}");
            sb.AppendLine();
        }

        sb.AppendLine($"— {appName}");
        sb.AppendLine();
        sb.AppendLine(
            "Вы получили это письмо, потому что включили напоминания о годовщинах " +
            "для этого человека. Отключить их можно в карточке умершего в приложении.");
        return sb.ToString();
    }

    private static string BuildHtml(
        string greeting,
        string lead,
        string reminder,
        string appName,
        string? appUrl)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;");
        sb.Append("max-width:520px;margin:0 auto;color:#1f2933;line-height:1.5;\">");
        sb.Append($"<p>{Enc(greeting)}</p>");
        sb.Append($"<p style=\"font-size:17px;\"><strong>{Enc(lead)}</strong></p>");
        sb.Append($"<p style=\"color:#52606d;\">{Enc(reminder)}</p>");

        if (!string.IsNullOrWhiteSpace(appUrl))
        {
            var safeUrl = Enc(appUrl);
            sb.Append(
                $"<p><a href=\"{safeUrl}\" style=\"display:inline-block;padding:10px 18px;" +
                "background:#2f6fed;color:#ffffff;text-decoration:none;border-radius:8px;\">" +
                "Открыть в приложении</a></p>");
        }

        sb.Append($"<p style=\"margin-top:28px;color:#52606d;\">— {Enc(appName)}</p>");
        sb.Append(
            "<hr style=\"border:none;border-top:1px solid #e4e7eb;margin:20px 0;\"/>" +
            "<p style=\"font-size:12px;color:#9aa5b1;\">Вы получили это письмо, потому что включили " +
            "напоминания о годовщинах для этого человека. Отключить их можно в карточке умершего " +
            "в приложении.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
