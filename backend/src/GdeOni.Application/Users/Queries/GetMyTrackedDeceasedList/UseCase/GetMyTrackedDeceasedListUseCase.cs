using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.UseCase;

public sealed class GetMyTrackedDeceasedListUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetMyTrackedDeceasedListUseCase
{
    public Task<Result<PagedResponse<MyTrackedDeceasedListItemResponse>, Error>> Execute(
        GetMyTrackedDeceasedListQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<PagedResponse<MyTrackedDeceasedListItemResponse>, Error>> Handle(
        GetMyTrackedDeceasedListQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var (pairs, totalCount) = await userRepository.GetMyTrackedDeceasedPaged(
            currentUserIdResult.Value,
            query.Page,
            query.PageSize,
            cancellationToken);

        var items = pairs
            .Select(pair =>
            {
                var (tracking, deceased) = pair;
                var primaryPhoto = deceased.GetPrimaryPhoto();

                return new MyTrackedDeceasedListItemResponse
                {
                    TrackingId = tracking.Id,
                    DeceasedId = deceased.Id,
                    FullName = deceased.Name.FullName,
                    BirthDate = deceased.LifePeriod.BirthDate,
                    DeathDate = deceased.LifePeriod.DeathDate,
                    HasGraveLocation = deceased.BurialLocation is not null,
                    GraveLatitude = deceased.BurialLocation?.Latitude,
                    GraveLongitude = deceased.BurialLocation?.Longitude,
                    MainPhotoUrl = primaryPhoto?.Url,
                    RelationshipType = tracking.RelationshipType.ToString(),
                    Status = tracking.Status.ToString(),
                    NotifyOnDeathAnniversary = tracking.NotifyOnDeathAnniversary,
                    NotifyOnBirthAnniversary = tracking.NotifyOnBirthAnniversary,
                    TrackedAtUtc = tracking.TrackedAtUtc
                };
            })
            .ToList();

        var response = new PagedResponse<MyTrackedDeceasedListItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Result.Success<PagedResponse<MyTrackedDeceasedListItemResponse>, Error>(response);
    }
}
