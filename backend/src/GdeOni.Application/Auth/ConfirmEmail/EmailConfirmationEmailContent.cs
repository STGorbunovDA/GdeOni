using System.Net;
using System.Text;
using GdeOni.Application.Abstractions.Email;

namespace GdeOni.Application.Auth.ConfirmEmail;

/// <summary>
/// D45. Чистый построитель письма со ссылкой подтверждения email.
/// Зеркалит <c>PasswordResetEmailContent</c>: не знает ни про SMTP, ни
/// про БД — принимает готовые данные и возвращает
/// <see cref="EmailMessage"/>. Вынесен отдельно, чтобы покрыть
/// формулировки и сборку ссылки юнит-тестами.
/// </summary>
public static class EmailConfirmationEmailContent
{
    /// <summary>
    /// Собирает письмо со ссылкой подтверждения.
    /// </summary>
    /// <param name="recipientEmail">Адрес получателя.</param>
    /// <param name="recipientName">Имя для приветствия (может быть null).</param>
    /// <param name="confirmUrl">Готовая ссылка с токеном.</param>
    /// <param name="lifetimeHours">Сколько часов действует ссылка.</param>
    /// <param name="appName">Название сервиса для подписи.</param>
    public static EmailMessage Build(
        string recipientEmail,
        string? recipientName,
        string confirmUrl,
        int lifetimeHours,
        string appName)
    {
        var greeting = string.IsNullOrWhiteSpace(recipientName)
            ? "Здравствуйте!"
            : $"Здравствуйте, {recipientName.Trim()}!";

        var subject = $"Подтверждение адреса — {appName}";

        var lead =
            "Спасибо за регистрацию. Чтобы подтвердить, что этот адрес " +
            "принадлежит вам, перейдите по ссылке ниже.";

        var validity =
            $"Ссылка действует {lifetimeHours} {HoursWord(lifetimeHours)}.";

        // Если человек НЕ регистрировался — успокаиваем: без перехода по
        // ссылке ничего не произойдёт, аккаунт не активируется этим
        // адресом. Иначе письмо выглядит как чужая активность на почте.
        const string ignoreHint =
            "Если вы не регистрировались в сервисе — просто удалите это письмо. " +
            "Никаких действий не требуется.";

        var text = BuildText(greeting, lead, confirmUrl, validity, ignoreHint, appName);
        var html = BuildHtml(greeting, lead, confirmUrl, validity, ignoreHint, appName);

        return new EmailMessage(recipientEmail, recipientName, subject, text, html);
    }

    /// <summary>
    /// Склеивает базовый URL страницы с токеном. Токен url-safe
    /// (base64url из фабрики), но всё равно экранируем — базовый URL
    /// может уже содержать query-строку.
    /// </summary>
    public static string BuildConfirmUrl(string baseUrl, string token)
    {
        var trimmed = baseUrl.TrimEnd('/', '?', '&');
        var separator = trimmed.Contains('?') ? '&' : '?';
        return $"{trimmed}{separator}token={Uri.EscapeDataString(token)}";
    }

    private static string HoursWord(int count)
    {
        var n = Math.Abs(count);
        var mod100 = n % 100;
        var mod10 = n % 10;
        if (mod100 is >= 11 and <= 14) return "часов";
        return mod10 switch
        {
            1 => "час",
            >= 2 and <= 4 => "часа",
            _ => "часов",
        };
    }

    private static string BuildText(
        string greeting,
        string lead,
        string confirmUrl,
        string validity,
        string ignoreHint,
        string appName)
    {
        var sb = new StringBuilder();
        sb.AppendLine(greeting);
        sb.AppendLine();
        sb.AppendLine(lead);
        sb.AppendLine();
        sb.AppendLine(confirmUrl);
        sb.AppendLine();
        sb.AppendLine(validity);
        sb.AppendLine();
        sb.AppendLine(ignoreHint);
        sb.AppendLine();
        sb.AppendLine($"— {appName}");
        return sb.ToString();
    }

    private static string BuildHtml(
        string greeting,
        string lead,
        string confirmUrl,
        string validity,
        string ignoreHint,
        string appName)
    {
        var safeUrl = Enc(confirmUrl);

        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;");
        sb.Append("max-width:520px;margin:0 auto;color:#1f2933;line-height:1.5;\">");
        sb.Append($"<p>{Enc(greeting)}</p>");
        sb.Append($"<p style=\"font-size:17px;\"><strong>{Enc(lead)}</strong></p>");
        sb.Append(
            $"<p><a href=\"{safeUrl}\" style=\"display:inline-block;padding:12px 22px;" +
            "background:#2f6fed;color:#ffffff;text-decoration:none;border-radius:8px;\">" +
            "Подтвердить email</a></p>");

        // Дублируем ссылку текстом: часть почтовых клиентов режет кнопки.
        sb.Append(
            "<p style=\"font-size:12px;color:#9aa5b1;word-break:break-all;\">" +
            $"Если кнопка не работает, скопируйте адрес: {safeUrl}</p>");

        sb.Append($"<p style=\"color:#52606d;\">{Enc(validity)}</p>");
        sb.Append(
            "<hr style=\"border:none;border-top:1px solid #e4e7eb;margin:20px 0;\"/>" +
            $"<p style=\"font-size:12px;color:#9aa5b1;\">{Enc(ignoreHint)}</p>");
        sb.Append($"<p style=\"margin-top:16px;color:#52606d;\">— {Enc(appName)}</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
