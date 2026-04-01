using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.GetTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.GetTracking.UseCase;

public sealed class GetTrackingUseCase(IUserRepository userRepository)
    : IGetTrackingUseCase
{
    public async Task<Result<GetTrackingResponse, Error>> Execute(
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

        var response = new GetTrackingResponse
        {
            Id = tracking.Id,
            UserId = user.Id,
            DeceasedId = tracking.DeceasedId,
            RelationshipType = (int)tracking.RelationshipType,
            PersonalNotes = tracking.PersonalNotes,
            NotifyOnDeathAnniversary = tracking.NotifyOnDeathAnniversary,
            NotifyOnBirthAnniversary = tracking.NotifyOnBirthAnniversary,
            HasNotificationsEnabled = tracking.HasNotificationsEnabled(),
            Status = (int)tracking.Status,
            TrackedAtUtc = tracking.TrackedAtUtc,
            IsActive = tracking.IsActive(),
            IsMuted = tracking.IsMuted(),
            IsArchived = tracking.IsArchived()
        };

        return Result.Success<GetTrackingResponse, Error>(response);
    }
}