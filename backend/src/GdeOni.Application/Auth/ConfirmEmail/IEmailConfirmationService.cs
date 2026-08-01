using GdeOni.Application.Abstractions.Email;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Auth.ConfirmEmail;

/// <summary>
/// D45. Единая точка политики подтверждения email: и гейт входа, и выдача
/// со отправкой ссылки. Держит вместе всё, что зависит от готовности
/// почтового канала (<see cref="IEmailSender.IsEnabled"/> +
/// <c>EmailConfirmationOptions.IsConfigured</c>), чтобы это правило не
/// расползлось по use case'ам.
/// </summary>
public interface IEmailConfirmationService
{
    /// <summary>
    /// Готов ли канал подтверждения: SMTP включён И задан
    /// <c>WebConfirmUrl</c>. Пока не готов — письма со ссылкой физически
    /// не уходят, поэтому гейт входа отключается (иначе новых юзеров
    /// нечем было бы разблокировать; заодно это спасает dev/тесты без
    /// SMTP от тотального локаута).
    /// </summary>
    bool ChannelReady { get; }

    /// <summary>
    /// Нужно ли отбить вход этому пользователю: он подчиняется гейту
    /// (<see cref="User.EmailConfirmationRequired"/>), ещё не подтвердил
    /// адрес и канал готов. «Старые» пользователи (Required=false)
    /// сюда не попадают никогда — у них только баннер.
    /// </summary>
    bool IsLoginBlocked(User user);

    /// <summary>
    /// Выписывает свежий токен подтверждения на пользователя (мутация —
    /// вызывающий обязан сам сделать Save) и возвращает готовое письмо,
    /// которое нужно отправить <b>после</b> Save. Тот же порядок, что в
    /// ForgotPasswordUseCase: при откате транзакции ссылка не должна вести
    /// на несохранённый токен.
    ///
    /// Возвращает null и НИЧЕГО не мутирует, если слать нечего: адрес уже
    /// подтверждён либо канал не готов.
    /// </summary>
    EmailMessage? IssueConfirmation(User user, DateTime nowUtc);

    /// <summary>
    /// Отправляет письмо. Ошибки доставки гасит и логирует (не бросает):
    /// токен уже сохранён, человек нажмёт «отправить повторно», а падать
    /// из-за недоступного SMTP регистрация/вход не должны.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
