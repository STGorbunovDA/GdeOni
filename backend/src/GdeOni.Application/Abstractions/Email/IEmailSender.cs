namespace GdeOni.Application.Abstractions.Email;

/// <summary>
/// D37. Абстракция канала отправки email. Реализуется в Infrastructure
/// (SMTP через System.Net.Mail) либо no-op заглушкой, когда почтовый
/// сервер не сконфигурирован (dev / integration-тесты).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// false, если канал не сконфигурирован (нет SMTP-хоста) и письма
    /// физически не уходят. Вызывающий (фоновый сервис годовщин) обязан
    /// проверить флаг ПЕРЕД тем как писать в лог отправленных — иначе,
    /// когда SMTP включат позже, годовщина уже будет помечена как
    /// «разослана» и реальное письмо не уйдёт.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Отправляет письмо. При <see cref="IsEnabled"/> = false —
    /// no-op (только debug-лог). При сбое доставки бросает исключение,
    /// чтобы вызывающий не пометил уведомление отправленным и повторил
    /// на следующем прогоне.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
