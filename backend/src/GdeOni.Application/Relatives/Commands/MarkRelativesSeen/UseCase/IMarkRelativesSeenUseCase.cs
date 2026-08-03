using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.MarkRelativesSeen.UseCase;

public interface IMarkRelativesSeenUseCase
{
    Task<UnitResult<Error>> Execute(CancellationToken cancellationToken);
}
