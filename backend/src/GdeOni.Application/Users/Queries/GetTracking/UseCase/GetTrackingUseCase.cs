using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Queries.GetTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetTracking.UseCase;

public sealed class GetTrackingUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetTrackingUseCase
{
    public Task<Result<GetTrackingResponse, Error>> Execute(
        GetTrackingQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetTrackingResponse, Error>> Handle(
        GetTrackingQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
            return Errors.General.Unauthorized();

        var currentUserId = currentUserService.UserId.Value;

        var user = await userRepository.GetByIdWithTracking(currentUserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserId);

        var tracking = user.GetTracking(query.DeceasedId);
        if (tracking is null)
            return Errors.Tracking.NotFound(query.DeceasedId);

        var response = new GetTrackingResponse
        {
            Id = tracking.Id,
            UserId = user.Id,
            DeceasedId = tracking.DeceasedId,
            RelationshipType = tracking.RelationshipType.ToString(),
            PersonalNotes = tracking.PersonalNotes,
            NotifyOnDeathAnniversary = tracking.NotifyOnDeathAnniversary,
            NotifyOnBirthAnniversary = tracking.NotifyOnBirthAnniversary,
            HasNotificationsEnabled = tracking.HasNotificationsEnabled(),
            Status = tracking.Status.ToString(),
            TrackedAtUtc = tracking.TrackedAtUtc,
            IsActive = tracking.IsActive(),
            IsMuted = tracking.IsMuted(),
            IsArchived = tracking.IsArchived()
        };

        return Result.Success<GetTrackingResponse, Error>(response);
    }
}