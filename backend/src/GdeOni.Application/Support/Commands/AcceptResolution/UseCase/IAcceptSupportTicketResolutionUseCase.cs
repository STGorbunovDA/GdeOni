using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.AcceptResolution.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.AcceptResolution.UseCase;

public interface IAcceptSupportTicketResolutionUseCase
{
    Task<UnitResult<Error>> Execute(
        AcceptSupportTicketResolutionCommand command,
        CancellationToken cancellationToken);
}
