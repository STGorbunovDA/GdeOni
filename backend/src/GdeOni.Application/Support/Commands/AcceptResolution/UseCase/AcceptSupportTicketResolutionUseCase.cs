using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.AcceptResolution.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.AcceptResolution.UseCase;

/// <summary>
/// D25. Юзер закрепляет резолюцию ("Закрепить решено"). Доступно
/// только автору тикета. Доменные правила: только Resolved-тикет
/// можно закрепить; повторный Accept → AlreadyAccepted.
/// </summary>
public sealed class AcceptSupportTicketResolutionUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IAcceptSupportTicketResolutionUseCase
{
    public async Task<UnitResult<Error>> Execute(
        AcceptSupportTicketResolutionCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var ticket = await ticketRepository.GetById(command.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.General.NotFound("support_ticket", command.TicketId);

        var acceptResult = ticket.AcceptResolution(
            currentUserIdResult.Value,
            timeProvider.GetUtcNow().UtcDateTime);

        if (acceptResult.IsFailure)
            return acceptResult.Error;

        await ticketRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
