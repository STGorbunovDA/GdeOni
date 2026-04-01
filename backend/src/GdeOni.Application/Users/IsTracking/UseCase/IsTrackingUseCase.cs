using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.IsTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.IsTracking.UseCase;

public sealed class IsTrackingUseCase(IUserRepository userRepository)
    : IIsTrackingUseCase
{
    public async Task<Result<IsTrackingResponse, Error>> Execute(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var response = new IsTrackingResponse
        {
            UserId = user.Id,
            DeceasedId = deceasedId,
            IsTracking = user.IsTracking(deceasedId)
        };

        return Result.Success<IsTrackingResponse, Error>(response);
    }
}