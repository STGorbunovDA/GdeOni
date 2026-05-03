using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Mappers;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.UseCase;

public sealed class GetMyTrackedDeceasedDetailsUseCase(
    IUserRepository userRepository,
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetMyTrackedDeceasedDetailsUseCase
{
    public Task<Result<MyTrackedDeceasedDetailsResponse, Error>> Execute(
        GetMyTrackedDeceasedDetailsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<MyTrackedDeceasedDetailsResponse, Error>> Handle(
        GetMyTrackedDeceasedDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D7.55: filtered Include — грузим только tracking по DeceasedId.
        var user = await userRepository.GetByIdWithTrackingByDeceasedId(
            currentUserIdResult.Value, query.DeceasedId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        var tracking = user.GetTracking(query.DeceasedId);
        if (tracking is null || tracking.IsArchived())
            return Errors.Tracking.NotTracked();

        var deceased = await deceasedRepository.GetByIdWithMemories(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        var canSeeAllMemories = currentUserService.IsAdmin()
            || deceased.CreatedByUserId == currentUserIdResult.Value;

        var response = new MyTrackedDeceasedDetailsResponse
        {
            Deceased = deceased.ToResponse(canSeeAllMemories),
            Tracking = new MyTrackingInfoResponse
            {
                TrackingId = tracking.Id,
                RelationshipType = tracking.RelationshipType.ToString(),
                PersonalNotes = tracking.PersonalNotes,
                NotifyOnDeathAnniversary = tracking.NotifyOnDeathAnniversary,
                NotifyOnBirthAnniversary = tracking.NotifyOnBirthAnniversary,
                Status = tracking.Status.ToString(),
                TrackedAtUtc = tracking.TrackedAtUtc
            }
        };

        return Result.Success<MyTrackedDeceasedDetailsResponse, Error>(response);
    }
}
