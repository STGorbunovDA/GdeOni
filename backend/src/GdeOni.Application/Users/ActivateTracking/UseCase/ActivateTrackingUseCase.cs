using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.ActivateTracking.UseCase;

public sealed class ActivateTrackingUseCase(IUserRepository userRepository)
    : IActivateTrackingUseCase
{
    public async Task<UnitResult<Error>> Execute(Guid userId, Guid deceasedId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var result = user.ActivateTracking(deceasedId);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}