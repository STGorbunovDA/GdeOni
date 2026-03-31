using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.GetAll.UseCase;

public sealed class GetAllDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetAllDeceasedUseCase
{
    public Task<Result<PagedResponse<DeceasedListItemResponse>, Error>> Execute(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(
            query,
            Handle,
            cancellationToken);
    }

    private async Task<Result<PagedResponse<DeceasedListItemResponse>, Error>> Handle(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await deceasedRepository.GetPaged(query, cancellationToken);

        var responseItems = items.Select(x => new DeceasedListItemResponse
        {
            Id = x.Id,
            FullName = x.Name.FullName,
            BirthDate = x.LifePeriod.BirthDate,
            DeathDate = x.LifePeriod.DeathDate,
            Country = x.BurialLocation.Country,
            City = x.BurialLocation.City,
            PlotNumber = x.BurialLocation.PlotNumber,
            GraveNumber = x.BurialLocation.GraveNumber,
            CemeteryName = x.BurialLocation.CemeteryName,
            IsVerified = x.IsVerified,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();

        var response = new PagedResponse<DeceasedListItemResponse>
        {
            Items = responseItems,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Result.Success<PagedResponse<DeceasedListItemResponse>, Error>(response);
    }
}