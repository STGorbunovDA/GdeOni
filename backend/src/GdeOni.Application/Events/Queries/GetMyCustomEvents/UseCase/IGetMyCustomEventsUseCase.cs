using CSharpFunctionalExtensions;
using GdeOni.Application.Events.Queries.GetMyCustomEvents.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Queries.GetMyCustomEvents.UseCase;

public interface IGetMyCustomEventsUseCase
{
    Task<Result<GetMyCustomEventsResponse, Error>> Execute(CancellationToken cancellationToken);
}
