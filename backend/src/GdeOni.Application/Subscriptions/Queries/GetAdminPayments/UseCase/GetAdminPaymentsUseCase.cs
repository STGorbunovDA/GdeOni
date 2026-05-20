using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.Model;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetAdminPayments.UseCase;

/// <summary>
/// D23. Админ-история платежей. Authorization на API ([Authorize(Roles=...)]);
/// здесь дополнительная проверка IsAdmin как defence-in-depth.
/// </summary>
public sealed class GetAdminPaymentsUseCase(
    ISubscriptionPaymentRepository paymentRepository,
    ICurrentUserService currentUserService) : IGetAdminPaymentsUseCase
{
    public async Task<Result<PagedPaymentsResponse, Error>> Execute(
        GetAdminPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var (items, totalCount) = await paymentRepository.GetPagedForAdmin(
            query.UserId,
            query.Status,
            query.CreatedFromUtc,
            query.CreatedToUtc,
            page,
            pageSize,
            cancellationToken);

        var response = new PagedPaymentsResponse(
            items.Select(x => PaymentRecordResponse.FromDomain(x.Payment, x.UserEmail)).ToList(),
            totalCount,
            page,
            pageSize);

        return Result.Success<PagedPaymentsResponse, Error>(response);
    }
}
