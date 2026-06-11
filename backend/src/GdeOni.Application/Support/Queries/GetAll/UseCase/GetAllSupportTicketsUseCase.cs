using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Queries.Common;
using GdeOni.Application.Support.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetAll.UseCase;

public sealed class GetAllSupportTicketsUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetAllSupportTicketsUseCase
{
    public Task<Result<GetAllSupportTicketsResponse, Error>> Execute(
        GetAllSupportTicketsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetAllSupportTicketsResponse, Error>> Handle(
        GetAllSupportTicketsQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var (rows, total) = await ticketRepository.GetPagedForAdmin(
            query.UserId,
            query.Statuses,
            query.Severities,
            query.Kind,
            query.Source,
            query.CreatedFromUtc,
            query.CreatedToUtc,
            query.Search,
            query.Page,
            query.PageSize,
            cancellationToken);

        var items = rows.Select(r => r.Ticket.ToDto(r.UserEmail)).ToList();

        return Result.Success<GetAllSupportTicketsResponse, Error>(
            new GetAllSupportTicketsResponse(items, total, query.Page, query.PageSize));
    }
}
