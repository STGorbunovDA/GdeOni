using CSharpFunctionalExtensions;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Model;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetAdminPayments.UseCase;

public interface IGetAdminPaymentsUseCase
{
    Task<Result<PagedPaymentsResponse, Error>> Execute(
        GetAdminPaymentsQuery query,
        CancellationToken cancellationToken);
}
