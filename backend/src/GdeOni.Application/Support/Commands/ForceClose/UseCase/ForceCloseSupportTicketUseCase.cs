using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.ForceClose.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.ForceClose.UseCase;

/// <summary>
/// D40. Принудительное закрытие обращения админом.
///
/// Нужно потому, что Resolved не терминален: точку в нём ставит юзер
/// (AcceptResolution), а он может просто забыть — и обращение висит
/// в списке админа вечно.
///
/// Роль проверяется и здесь, а не только атрибутом на контроллере —
/// defense in depth, как в остальных админских use case'ах.
/// </summary>
public sealed class ForceCloseSupportTicketUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IForceCloseSupportTicketUseCase
{
    public Task<UnitResult<Error>> Execute(
        ForceCloseSupportTicketCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        ForceCloseSupportTicketCommand command,
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

        var closeResult = ticket.ForceClose(
            currentUserIdResult.Value,
            command.CloseNote,
            timeProvider.GetUtcNow().UtcDateTime);

        if (closeResult.IsFailure)
            return closeResult.Error;

        await ticketRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
