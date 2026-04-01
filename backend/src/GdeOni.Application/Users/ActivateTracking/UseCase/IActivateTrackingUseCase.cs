using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ActivateTracking.UseCase;

public interface IActivateTrackingUseCase
{
    Task<UnitResult<Error>> Execute(Guid userId, Guid deceasedId, CancellationToken cancellationToken);
}