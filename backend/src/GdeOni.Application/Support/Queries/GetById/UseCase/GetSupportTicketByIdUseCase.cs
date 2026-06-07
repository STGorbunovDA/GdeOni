using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Queries.Common;
using GdeOni.Application.Support.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetById.UseCase;

/// <summary>
/// D25. Карточка тикета. Admin видит любой; обычный юзер — только
/// свой (ViewForbidden иначе). Так мы переиспользуем один use case
/// и для "Мои обращения / детали" в мобильном клиенте, и для
/// "Карточка тикета" в админке.
/// </summary>
public sealed class GetSupportTicketByIdUseCase(
    ISupportTicketRepository ticketRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : IGetSupportTicketByIdUseCase
{
    public async Task<Result<GetSupportTicketByIdResponse, Error>> Execute(
        GetSupportTicketByIdQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var ticket = await ticketRepository.GetById(query.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.General.NotFound("support_ticket", query.TicketId);

        var isAdmin = currentUserService.IsAdmin();
        if (!isAdmin && ticket.UserId != currentUserIdResult.Value)
            return Errors.Support.ViewForbidden();

        // Email берём только для админа — обычному юзеру свой email
        // ни к чему (в карточке он не показывается).
        string? userEmail = null;
        if (isAdmin && ticket.UserId is { } userId)
        {
            userEmail = await userRepository.GetEmailById(userId, cancellationToken);
        }

        return Result.Success<GetSupportTicketByIdResponse, Error>(
            new GetSupportTicketByIdResponse(ticket.ToDto(userEmail)));
    }
}
