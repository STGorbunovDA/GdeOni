using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetMyPayments.UseCase;

public sealed class GetMyPaymentsUseCase(
    ISubscriptionPaymentRepository paymentRepository,
    ICurrentUserService currentUserService) : IGetMyPaymentsUseCase
{
    public async Task<Result<PagedPaymentsResponse, Error>> Execute(
        GetMyPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var (items, totalCount) = await paymentRepository.GetPagedForUser(
            currentUserIdResult.Value, page, pageSize, cancellationToken);

        var response = new PagedPaymentsResponse(
            items.Select(p => PaymentRecordResponse.FromDomain(p)).ToList(),
            totalCount,
            page,
            pageSize);

        return Result.Success<PagedPaymentsResponse, Error>(response);
    }
}
