using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Queries.Common;
using GdeOni.Application.Support.Queries.GetMine.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetMine.UseCase;

/// <summary>
/// D25. "Мои обращения" — листинг тикетов текущего юзера. Юзер видит
/// свои Manual-тикеты + auto-инциденты, привязанные к нему.
/// </summary>
public sealed class GetMySupportTicketsUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetMySupportTicketsUseCase
{
    public Task<Result<GetMySupportTicketsResponse, Error>> Execute(
        GetMySupportTicketsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetMySupportTicketsResponse, Error>> Handle(
        GetMySupportTicketsQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var (items, total) = await ticketRepository.GetPagedForUser(
            currentUserIdResult.Value,
            query.Page,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(t => t.ToDto()).ToList();

        return Result.Success<GetMySupportTicketsResponse, Error>(
            new GetMySupportTicketsResponse(dtos, total, query.Page, query.PageSize));
    }
}
