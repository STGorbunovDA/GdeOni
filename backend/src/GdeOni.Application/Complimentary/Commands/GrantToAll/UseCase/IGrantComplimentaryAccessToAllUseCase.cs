using CSharpFunctionalExtensions;
using GdeOni.Application.Complimentary.Commands.GrantToAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Complimentary.Commands.GrantToAll.UseCase;

public interface IGrantComplimentaryAccessToAllUseCase
{
    Task<Result<GrantComplimentaryAccessToAllResponse, Error>> Execute(
        GrantComplimentaryAccessToAllCommand command,
        CancellationToken cancellationToken);
}
