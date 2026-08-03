using CSharpFunctionalExtensions;
using GdeOni.Application.Events.Commands.CreateCustomEvent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Commands.CreateCustomEvent.UseCase;

public interface ICreateCustomEventUseCase
{
    Task<Result<CreateCustomEventResponse, Error>> Execute(
        CreateCustomEventCommand command, CancellationToken cancellationToken);
}
