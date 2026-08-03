using CSharpFunctionalExtensions;
using GdeOni.Application.Events.Commands.UpdateCustomEvent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Commands.UpdateCustomEvent.UseCase;

public interface IUpdateCustomEventUseCase
{
    Task<UnitResult<Error>> Execute(
        UpdateCustomEventCommand command, CancellationToken cancellationToken);
}
