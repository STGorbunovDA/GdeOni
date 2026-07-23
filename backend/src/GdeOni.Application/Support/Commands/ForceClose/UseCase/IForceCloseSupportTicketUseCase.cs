using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.ForceClose.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.ForceClose.UseCase;

public interface IForceCloseSupportTicketUseCase
{
    Task<UnitResult<Error>> Execute(
        ForceCloseSupportTicketCommand command,
        CancellationToken cancellationToken);
}
