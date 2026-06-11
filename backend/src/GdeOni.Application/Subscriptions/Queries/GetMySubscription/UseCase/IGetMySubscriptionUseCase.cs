using CSharpFunctionalExtensions;
using GdeOni.Application.Subscriptions.Queries.GetMySubscription.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetMySubscription.UseCase;

public interface IGetMySubscriptionUseCase
{
    Task<Result<MySubscriptionResponse, Error>> Execute(CancellationToken cancellationToken);
}
