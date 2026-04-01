using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.MuteTracking.UseCase;

public interface IMuteTrackingUseCase
{
    Task<UnitResult<Error>> Execute(Guid userId, Guid deceasedId, CancellationToken cancellationToken);
}