using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.GetAll.UseCase;

public sealed class GetAllDeceasedUseCase(IDeceasedRepository deceasedRepository)
    : IGetAllDeceasedUseCase
{
    public async Task<Result<List<DeceasedListItemResponse>, Error>> Execute(CancellationToken cancellationToken)
    {
        var items = await deceasedRepository.GetAll(cancellationToken);

        var response = items.Select(x => new DeceasedListItemResponse
        {
            Id = x.Id,
            FullName = x.Name.FullName,
            BirthDate = x.LifePeriod.BirthDate,
            DeathDate = x.LifePeriod.DeathDate,
            Country = x.BurialLocation.Country,
            City = x.BurialLocation.City,
            CemeteryName = x.BurialLocation.CemeteryName,
            IsVerified = x.IsVerified,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();

        return Result.Success<List<DeceasedListItemResponse>, Error>(response);
    }
}