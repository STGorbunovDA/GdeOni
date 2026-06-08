using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.Reopen.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.Reopen.UseCase;

public interface IReopenSupportTicketUseCase
{
    Task<UnitResult<Error>> Execute(
        ReopenSupportTicketCommand command,
        CancellationToken cancellationToken);
}
