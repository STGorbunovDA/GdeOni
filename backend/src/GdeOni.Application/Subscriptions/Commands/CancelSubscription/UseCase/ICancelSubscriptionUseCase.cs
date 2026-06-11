using CSharpFunctionalExtensions;
using GdeOni.Application.Subscriptions.Commands.CancelSubscription.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.CancelSubscription.UseCase;

public interface ICancelSubscriptionUseCase
{
    Task<UnitResult<Error>> Execute(
        CancelSubscriptionCommand command,
        CancellationToken cancellationToken);
}
