using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
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
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor) : IGetAdminPaymentsUseCase
{
    public Task<Result<PagedPaymentsResponse, Error>> Execute(
        GetAdminPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<PagedPaymentsResponse, Error>> Handle(
        GetAdminPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        // Inline clamping убран — валидация в GetAdminPaymentsQueryValidator.
        var (items, totalCount) = await paymentRepository.GetPagedForAdmin(
            query.UserId,
            query.Status,
            query.CreatedFromUtc,
            query.CreatedToUtc,
            query.EmailSearch,
            query.Page,
            query.PageSize,
            cancellationToken);

        var response = new PagedPaymentsResponse(
            items.Select(x => PaymentRecordResponse.FromDomain(x.Payment, x.UserEmail)).ToList(),
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success<PagedPaymentsResponse, Error>(response);
    }
}
