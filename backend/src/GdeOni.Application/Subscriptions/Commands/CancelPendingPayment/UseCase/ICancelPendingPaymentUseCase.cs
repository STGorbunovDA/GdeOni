using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.CancelPendingPayment.UseCase;

/// <summary>
/// D16. Юзер тапнул «Отменить» при зависшем PendingPayment (например,
/// нажал «Назад» на странице YooKassa). Use case:
///   1) находит свежий Pending платёж юзера;
///   2) отменяет его в YooKassa (POST /v3/payments/{id}/cancel);
///   3) помечает <c>SubscriptionPayment.Cancelled</c>;
///   4) откатывает <c>User.Subscription</c> в Trial/Expired
///      (см. <see cref="GdeOni.Domain.Aggregates.User.User.CancelPendingPayment"/>).
/// </summary>
public interface ICancelPendingPaymentUseCase
{
    Task<UnitResult<Error>> Execute(CancellationToken cancellationToken);
}
