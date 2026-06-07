using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.UpdateStatus.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.UpdateStatus.UseCase;

public interface IUpdateSupportTicketStatusUseCase
{
    Task<UnitResult<Error>> Execute(
        UpdateSupportTicketStatusCommand command,
        CancellationToken cancellationToken);
}
