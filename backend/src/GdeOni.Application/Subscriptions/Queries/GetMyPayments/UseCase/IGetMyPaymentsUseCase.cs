using CSharpFunctionalExtensions;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetMyPayments.UseCase;

public interface IGetMyPaymentsUseCase
{
    Task<Result<PagedPaymentsResponse, Error>> Execute(
        GetMyPaymentsQuery query,
        CancellationToken cancellationToken);
}
