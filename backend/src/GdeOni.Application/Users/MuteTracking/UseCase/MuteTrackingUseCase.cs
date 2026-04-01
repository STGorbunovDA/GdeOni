using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.MuteTracking.UseCase;

public sealed class MuteTrackingUseCase(IUserRepository userRepository)
    : IMuteTrackingUseCase
{
    public async Task<UnitResult<Error>> Execute(Guid userId, Guid deceasedId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var result = user.MuteTracking(deceasedId);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}