namespace GdeOni.Application.Abstractions.Email;

/// <summary>
/// D37. Одно исходящее письмо. Транспорт-агностично: ни SMTP, ни
/// провайдер здесь не фигурируют — это делает <see cref="IEmailSender"/>.
///
/// Тело задаётся сразу в двух представлениях: <see cref="TextBody"/>
/// (обязателен) и <see cref="HtmlBody"/> (опционален). Почтовые клиенты,
/// не умеющие HTML, показывают plain-text — поэтому текстовая версия
/// всегда должна быть осмысленной.
/// </summary>
public sealed record EmailMessage(
    string ToEmail,
    string? ToName,
    string Subject,
    string TextBody,
    string? HtmlBody = null);
