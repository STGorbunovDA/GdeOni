using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.Create.UseCase;

public interface ICreateSupportTicketUseCase
{
    Task<Result<CreateSupportTicketResponse, Error>> Execute(
        CreateSupportTicketCommand command,
        CancellationToken cancellationToken);
}
