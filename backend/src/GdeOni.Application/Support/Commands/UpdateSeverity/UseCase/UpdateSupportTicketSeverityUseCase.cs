using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.UpdateSeverity.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.UpdateSeverity.UseCase;

public sealed class UpdateSupportTicketSeverityUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IUpdateSupportTicketSeverityUseCase
{
    public Task<UnitResult<Error>> Execute(
        UpdateSupportTicketSeverityCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        UpdateSupportTicketSeverityCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var ticket = await ticketRepository.GetById(command.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.General.NotFound("support_ticket", command.TicketId);

        var changeResult = ticket.ChangeSeverity(
            command.Severity,
            timeProvider.GetUtcNow().UtcDateTime);

        if (changeResult.IsFailure)
            return changeResult.Error;

        await ticketRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
