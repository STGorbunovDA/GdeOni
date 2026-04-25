using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAll.UseCase;

public sealed class GetAllDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetAllDeceasedUseCase
{
    public Task<Result<PagedResponse<GetAllDeceasedItemResponse>, Error>> Execute(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<PagedResponse<GetAllDeceasedItemResponse>, Error>> Handle(
        GetAllDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
            return Errors.General.Unauthorized();
        
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(),
            UserRole.Admin.ToString());

        if (!isAdmin)
            return Errors.Deceased.InsufficientPermissionsToViewAllDeceased();
        
        var (items, totalCount) = await deceasedRepository.GetPaged(query, cancellationToken);

        var response = new PagedResponse<GetAllDeceasedItemResponse>
        {
            Items = items.Select(x => new GetAllDeceasedItemResponse
            {
                Id = x.Id,
                FullName = x.Name.FullName,
                BirthDate = x.LifePeriod.BirthDate,
                DeathDate = x.LifePeriod.DeathDate,
                Country = x.BurialLocation.Country,
                City = x.BurialLocation.City,
                CemeteryName = x.BurialLocation.CemeteryName,
                PlotNumber = x.BurialLocation.PlotNumber,
                GraveNumber = x.BurialLocation.GraveNumber,
                IsVerified = x.IsVerified,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Result.Success<PagedResponse<GetAllDeceasedItemResponse>, Error>(response);
    }
}