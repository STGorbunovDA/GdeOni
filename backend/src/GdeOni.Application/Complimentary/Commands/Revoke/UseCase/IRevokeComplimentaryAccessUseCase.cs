using CSharpFunctionalExtensions;
using GdeOni.Application.Complimentary.Commands.Revoke.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Complimentary.Commands.Revoke.UseCase;

public interface IRevokeComplimentaryAccessUseCase
{
    Task<UnitResult<Error>> Execute(
        RevokeComplimentaryAccessCommand command,
        CancellationToken cancellationToken);
}
