using CSharpFunctionalExtensions;
using GdeOni.Application.Legal.Commands.AcceptLegal.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Legal.Commands.AcceptLegal.UseCase;

public interface IAcceptLegalUseCase
{
    Task<UnitResult<Error>> Execute(AcceptLegalCommand command, CancellationToken cancellationToken);
}
