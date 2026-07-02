using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.SyncSubscription.UseCase;

/// <summary>
/// D16. Pull-fallback вместо webhook. Юзер тапнул «обновить статус»
/// после оплаты (или клиент дёргает автоматически при
/// <c>PendingPayment</c>) — бэк идёт к YooKassa за реальным статусом,
/// применяет ту же логику, что webhook-хендлер.
/// </summary>
public interface ISyncSubscriptionUseCase
{
    Task<UnitResult<Error>> Execute(CancellationToken cancellationToken);
}
