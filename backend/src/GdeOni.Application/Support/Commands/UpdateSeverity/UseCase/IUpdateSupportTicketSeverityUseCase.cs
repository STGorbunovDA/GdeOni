using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.UpdateSeverity.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.UpdateSeverity.UseCase;

public interface IUpdateSupportTicketSeverityUseCase
{
    Task<UnitResult<Error>> Execute(
        UpdateSupportTicketSeverityCommand command,
        CancellationToken cancellationToken);
}
