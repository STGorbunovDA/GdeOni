using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.UseCase;

public sealed class GetNearbyDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetNearbyDeceasedUseCase
{
    public Task<Result<PagedResponse<NearbyDeceasedItemResponse>, Error>> Execute(
        GetNearbyDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<PagedResponse<NearbyDeceasedItemResponse>, Error>> Handle(
        GetNearbyDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        // Только авторизованный юзер — как и GetAll (D15). Поиск "рядом"
        // отдаёт более точные геоданные, чем GetAll, но политика та же:
        // регистрированному всё видно, чтобы он мог подписаться.
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var (items, totalCount) = await deceasedRepository.GetNearby(query, cancellationToken);

        var response = new PagedResponse<NearbyDeceasedItemResponse>
        {
            Items = items
                .Select(x => new NearbyDeceasedItemResponse
                {
                    Id = x.Deceased.Id,
                    FullName = x.Deceased.Name.FullName,
                    BirthDate = x.Deceased.LifePeriod.BirthDate,
                    DeathDate = x.Deceased.LifePeriod.DeathDate,
                    Latitude = x.Deceased.BurialLocation!.Latitude,
                    Longitude = x.Deceased.BurialLocation.Longitude,
                    Country = x.Deceased.BurialLocation.Country,
                    City = x.Deceased.BurialLocation.City,
                    CemeteryName = x.Deceased.BurialLocation.CemeteryName,
                    PlotNumber = x.Deceased.BurialLocation.PlotNumber,
                    GraveNumber = x.Deceased.BurialLocation.GraveNumber,
                    IsVerified = x.Deceased.IsVerified,
                    DistanceMeters = (int)Math.Round(x.DistanceMeters),
                })
                .ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };

        return Result.Success<PagedResponse<NearbyDeceasedItemResponse>, Error>(response);
    }
}
