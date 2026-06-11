using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Payments;

/// <summary>
/// D16. Абстракция платёжного провайдера (YooKassa в проде,
/// Fake в dev/тестах). Все методы возвращают
/// <see cref="Result{T, TE}"/> — Failure для сетевых проблем,
/// невалидной подписи webhook'а и т.д.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Создаёт платёж в провайдере и возвращает URL для оплаты.
    /// Вызывается из <c>CreatePaymentUseCase</c>. AmountRub приходит
    /// из <c>SubscriptionOptions.MonthlyPriceRub</c> — провайдер не
    /// знает про доменные планы, только сумму и описание.
    /// </summary>
    /// <param name="userId">
    /// Кладётся в metadata платежа (для трассировки / поддержки), но
    /// не используется для маршрутизации — webhook ищет юзера по
    /// ExternalPaymentId.
    /// </param>
    /// <param name="amountRub">Сумма в рублях.</param>
    /// <param name="description">Текстовое описание для receipt'а.</param>
    /// <param name="returnUrl">
    /// URL возврата после оплаты (deep-link mobile или web URL).
    /// </param>
    Task<Result<PaymentCreated, Error>> CreateAsync(
        Guid userId,
        decimal amountRub,
        string description,
        string returnUrl,
        CancellationToken cancellationToken);

    /// <summary>
    /// Верифицирует webhook от провайдера и парсит payload.
    /// Failure при невалидной HMAC-подписи (или любой ошибке парсинга)
    /// → use case вернёт 401 без обращения к БД, защита от replay.
    /// </summary>
    /// <param name="payload">Сырое тело запроса (для HMAC verify).</param>
    /// <param name="signatureHeader">
    /// Заголовок с подписью (название зависит от провайдера; для
    /// YooKassa — Idempotence-Key + проверка через basic auth +
    /// allowed IP).
    /// </param>
    Task<Result<PaymentVerification, Error>> VerifyWebhookAsync(
        string payload,
        string? signatureHeader,
        CancellationToken cancellationToken);

    /// <summary>
    /// Отменяет автопродление у провайдера (если есть). Для YooKassa
    /// при single-payment-модели — no-op (мы просто не создаём
    /// следующий платёж). Метод оставлен для recurring-flow,
    /// если когда-нибудь подключим saved-card auto-charge.
    /// </summary>
    Task<UnitResult<Error>> CancelRecurringAsync(
        string externalPaymentId,
        CancellationToken cancellationToken);
}
