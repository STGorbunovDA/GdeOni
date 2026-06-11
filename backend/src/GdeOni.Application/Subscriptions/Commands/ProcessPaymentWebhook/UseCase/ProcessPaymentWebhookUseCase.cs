using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;

public sealed class ProcessPaymentWebhookUseCase(
    IUserRepository userRepository,
    ISubscriptionPaymentRepository paymentRepository,
    ISupportTicketRepository supportTicketRepository,
    IPaymentProvider paymentProvider,
    IOptions<SubscriptionOptions> subscriptionOptions,
    TimeProvider timeProvider)
    : IProcessPaymentWebhookUseCase
{
    public async Task<UnitResult<Error>> Execute(
        ProcessPaymentWebhookCommand command,
        CancellationToken cancellationToken)
    {
        // Шаг 1: верификация подписи. Если фейл — Failure уходит в
        // контроллер, 401 без обращения к БД. Защита от replay-атак.
        var verificationResult = await paymentProvider.VerifyWebhookAsync(
            command.Payload,
            command.SignatureHeader,
            cancellationToken);

        if (verificationResult.IsFailure)
            return verificationResult.Error;

        var verification = verificationResult.Value;

        // D23. Шаг 2: поиск платежа в субscription_payments. Раньше
        // искали через User.Subscription.LastPaymentId, но он
        // перезаписывался при каждом CreatePayment — multi-tap ломал
        // привязку. Теперь источник истины — отдельная запись с
        // unique-индексом по external_payment_id.
        var payment = await paymentRepository.GetByExternalPaymentId(
            verification.ExternalPaymentId, cancellationToken);
        if (payment is null)
        {
            // D25. Auto-инцидент: YooKassa подтвердила платёж по
            // external_payment_id, которого в нашей БД нет. Это значит
            // либо CreatePayment упал после ответа провайдера (наш
            // INSERT не доехал, а YooKassa уже выставила счёт), либо
            // race с очень медленным INSERT'ом, либо подделка с валидной
            // подписью (что невероятно). User неизвестен — пишем тикет
            // без UserId, админ разберётся по external_payment_id.
            await CreateAutoTicketAsync(
                verification.ExternalPaymentId,
                verification.Status.ToString(),
                cancellationToken);
            return Errors.Subscription.PaymentNotFound();
        }

        // Pending от YooKassa — финальный статус ещё не известен.
        // Ничего не меняем; YooKassa пришлёт ещё один webhook позже.
        if (verification.Status == PaymentStatus.Pending)
            return UnitResult.Success<Error>();

        // Шаг 3: грузим юзера. Tracked-вариант — будем мутить
        // Subscription и saveChanges.
        var user = await userRepository.GetById(payment.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", payment.UserId);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Concurrent webhook race: YooKassa может прислать два webhook'а
        // с одним external_payment_id (retry-механизм). Оба попадут сюда
        // с почти одинаковым snapshot'ом payment.Status=Pending. Защита:
        // - MarkSucceeded имеет early-return на Status=Succeeded (idempotent);
        // - MarkCancelled — то же на Status=Cancelled;
        // - Cancelled→Succeeded или Succeeded→Cancelled блокируется
        //   AlreadyProcessed (409). YooKassa такой переход не делает.
        // - unique-индекс ux_subscription_payments_external_payment_id
        //   защищает от дубль-INSERT'а через webhook (если первый ещё
        //   не закоммитил).
        // Полностью атомарная сериализация — отдельный backlog
        // "SERIALIZABLE-транзакция + retry на 40001" (см. PlanFull).
        switch (verification.Status)
        {
            case PaymentStatus.Succeeded:
                var expiresAt = nowUtc + subscriptionOptions.Value.MonthlyDuration;

                var activateResult = user.ActivateSubscription(
                    payment.Plan, nowUtc, expiresAt, verification.ExternalPaymentId);
                if (activateResult.IsFailure)
                    return activateResult.Error;

                var markSucceededResult = payment.MarkSucceeded(nowUtc, expiresAt, nowUtc);
                if (markSucceededResult.IsFailure)
                    return markSucceededResult.Error;
                break;

            case PaymentStatus.Cancelled:
                // Платёж отменился на стороне YooKassa — Subscription
                // остаётся в PendingPayment / Trial. Юзер всегда может
                // сделать новый CreatePayment. Запись помечается
                // Cancelled — UI и админка увидят историю.
                var markCancelledResult = payment.MarkCancelled(nowUtc);
                if (markCancelledResult.IsFailure)
                    return markCancelledResult.Error;
                break;
        }

        // Один DbContext — Save фиксирует и User, и SubscriptionPayment
        // в одной транзакции.
        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }

    private async Task CreateAutoTicketAsync(
        string externalPaymentId,
        string yooKassaStatus,
        CancellationToken cancellationToken)
    {
        var details =
            $"{{\"externalPaymentId\":\"{externalPaymentId}\"," +
            $"\"yooKassaStatus\":\"{yooKassaStatus}\"}}";

        var ticketResult = SupportTicket.CreateAuto(
            userId: null,
            kind: SupportTicketKind.Payment,
            severity: SupportTicketSeverity.Urgent,
            title: "Webhook: платёж не найден в БД",
            description: $"YooKassa прислала webhook со статусом " +
                $"'{yooKassaStatus}' для external_payment_id " +
                $"'{externalPaymentId}', но в subscription_payments " +
                $"такой записи нет. Возможные причины: упал CreatePayment " +
                $"после ответа провайдера, race-условие, или внешняя " +
                $"попытка с валидной подписью. Найти юзера по " +
                $"external_payment_id в логах YooKassa.",
            details: details,
            nowUtc: timeProvider.GetUtcNow().UtcDateTime);

        if (ticketResult.IsFailure)
            return;

        await supportTicketRepository.Add(ticketResult.Value, cancellationToken);
        await supportTicketRepository.Save(cancellationToken);
    }
}
