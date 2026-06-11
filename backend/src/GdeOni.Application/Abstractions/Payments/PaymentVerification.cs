using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Payments;

/// <summary>
/// D16. Результат верификации webhook от <see cref="IPaymentProvider"/>.
/// Используется <c>ProcessPaymentWebhookUseCase</c>: если
/// <see cref="Result"/> Failure (например,
/// <c>Errors.Subscription.InvalidPaymentSignature</c>) — use case
/// сразу возвращает 401 без попыток найти юзера.
/// </summary>
/// <param name="ExternalPaymentId">
/// ID платежа в системе провайдера — по нему находим юзера через
/// <c>IUserRepository.GetBySubscriptionPaymentId</c>.
/// </param>
/// <param name="Status">Финальный статус платежа после провайдера.</param>
/// <param name="AmountRub">
/// Сумма в рублях. Сохраняется только для логирования / аудита —
/// проверка соответствия плана делается use case'ом.
/// </param>
public sealed record PaymentVerification(
    string ExternalPaymentId,
    PaymentStatus Status,
    decimal AmountRub);
