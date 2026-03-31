using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.StopTracking.UseCase;

public sealed class StopTrackingUseCase(IUserRepository userRepository)
    : IStopTrackingUseCase
{
    public async Task<UnitResult<Error>> Execute(Guid userId, Guid deceasedId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var result = user.StopTracking(deceasedId);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}