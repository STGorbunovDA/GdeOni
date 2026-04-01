using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.RemoveTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.RemoveTracking.UseCase;

public sealed class RemoveTrackingUseCase(IUserRepository userRepository)
    : IRemoveTrackingUseCase
{
    public async Task<Result<RemoveTrackingResponse, Error>> Execute(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var result = user.RemoveTracking(deceasedId);
        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        var response = new RemoveTrackingResponse
        {
            UserId = user.Id,
            DeceasedId = deceasedId,
            Removed = true
        };

        return Result.Success<RemoveTrackingResponse, Error>(response);
    }
}