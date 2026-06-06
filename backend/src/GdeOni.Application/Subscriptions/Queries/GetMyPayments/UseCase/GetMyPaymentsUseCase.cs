using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Queries.GetMyPayments.UseCase;

public sealed class GetMyPaymentsUseCase(
    ISubscriptionPaymentRepository paymentRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor) : IGetMyPaymentsUseCase
{
    public Task<Result<PagedPaymentsResponse, Error>> Execute(
        GetMyPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<PagedPaymentsResponse, Error>> Handle(
        GetMyPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // Inline clamping убран — пагинация валидируется в
        // GetMyPaymentsQueryValidator (Page>=1, PageSize 1..100).
        // Раньше Page=-5 молча приводился к 1, маскируя баг клиента.
        var (items, totalCount) = await paymentRepository.GetPagedForUser(
            currentUserIdResult.Value, query.Page, query.PageSize, cancellationToken);

        var response = new PagedPaymentsResponse(
            items.Select(p => PaymentRecordResponse.FromDomain(p)).ToList(),
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success<PagedPaymentsResponse, Error>(response);
    }
}
