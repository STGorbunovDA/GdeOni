using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.HasNotificationsEnabled.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.HasNotificationsEnabled.UseCase;

public sealed class HasNotificationsEnabledUseCase(IUserRepository userRepository)
    : IHasNotificationsEnabledUseCase
{
    public async Task<Result<HasNotificationsEnabledResponse, Error>> Execute(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var tracking = user.GetTracking(deceasedId);
        if (tracking is null)
            return Errors.Tracking.NotFound(deceasedId);

        var response = new HasNotificationsEnabledResponse
        {
            UserId = user.Id,
            DeceasedId = deceasedId,
            HasNotificationsEnabled = tracking.HasNotificationsEnabled()
        };

        return Result.Success<HasNotificationsEnabledResponse, Error>(response);
    }
}