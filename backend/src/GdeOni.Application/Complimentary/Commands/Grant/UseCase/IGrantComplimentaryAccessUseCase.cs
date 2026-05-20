using CSharpFunctionalExtensions;
using GdeOni.Application.Complimentary.Commands.Grant.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Complimentary.Commands.Grant.UseCase;

public interface IGrantComplimentaryAccessUseCase
{
    Task<UnitResult<Error>> Execute(
        GrantComplimentaryAccessCommand command,
        CancellationToken cancellationToken);
}
