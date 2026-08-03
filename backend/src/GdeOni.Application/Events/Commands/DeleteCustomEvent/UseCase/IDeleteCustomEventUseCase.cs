using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Commands.DeleteCustomEvent.UseCase;

public interface IDeleteCustomEventUseCase
{
    Task<UnitResult<Error>> Execute(Guid id, CancellationToken cancellationToken);
}
