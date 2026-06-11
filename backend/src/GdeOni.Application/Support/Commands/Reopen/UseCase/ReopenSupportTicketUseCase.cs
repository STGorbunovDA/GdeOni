using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.Reopen.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.Reopen.UseCase;

/// <summary>
/// D25. Юзер не согласен с резолюцией ("Продолжить спор"). Доступно
/// только автору. Доменные правила: только Resolved тикет можно
/// reopen; если юзер уже Accept'нул — нельзя (AlreadyAccepted).
/// </summary>
public sealed class ReopenSupportTicketUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IReopenSupportTicketUseCase
{
    public Task<UnitResult<Error>> Execute(
        ReopenSupportTicketCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        ReopenSupportTicketCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var ticket = await ticketRepository.GetById(command.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.General.NotFound("support_ticket", command.TicketId);

        var reopenResult = ticket.Reopen(
            currentUserIdResult.Value,
            command.UserReply,
            timeProvider.GetUtcNow().UtcDateTime);

        if (reopenResult.IsFailure)
            return reopenResult.Error;

        await ticketRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
