using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.UpdateStatus.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.UpdateStatus.UseCase;

/// <summary>
/// D25. Админская смена статуса тикета. Authorize-role проверяется на
/// контроллере, дополнительно <see cref="ICurrentUserService.IsAdmin"/>
/// в use case'е — defense in depth (если по ошибке мапнут роль не
/// через [Authorize], use case не пустит).
/// </summary>
public sealed class UpdateSupportTicketStatusUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IUpdateSupportTicketStatusUseCase
{
    public Task<UnitResult<Error>> Execute(
        UpdateSupportTicketStatusCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        UpdateSupportTicketStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var ticket = await ticketRepository.GetById(command.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.General.NotFound("support_ticket", command.TicketId);

        var changeResult = ticket.ChangeStatus(
            command.Status,
            currentUserIdResult.Value,
            command.ResolutionNote,
            timeProvider.GetUtcNow().UtcDateTime);

        if (changeResult.IsFailure)
            return changeResult.Error;

        await ticketRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
