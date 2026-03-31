using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.StopTracking.UseCase;

public interface IStopTrackingUseCase
{
    Task<UnitResult<Error>> Execute(Guid userId, Guid deceasedId, CancellationToken cancellationToken);
}