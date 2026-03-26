using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.GetById.UseCase;

public class GetDeceasedByIdUseCase (IDeceasedRepository deceasedRepository)
    : IGetDeceasedByIdUseCase
{
    public async Task<Result<DeceasedDetailsResponse, Error>> Execute(Guid id, CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(id, cancellationToken);

        if (deceased is null)
            return Errors.General.NotFound("deceased", id);

        var response = new DeceasedDetailsResponse
        {
            Id = deceased.Id,
            FirstName = deceased.Name.FirstName,
            LastName = deceased.Name.LastName,
            MiddleName = deceased.Name.MiddleName,
            FullName = deceased.Name.FullName,
            BirthDate = deceased.LifePeriod.BirthDate,
            DeathDate = deceased.LifePeriod.DeathDate,

            Latitude = deceased.BurialLocation.Latitude,
            Longitude = deceased.BurialLocation.Longitude,
            Country = deceased.BurialLocation.Country,
            Region = deceased.BurialLocation.Region,
            City = deceased.BurialLocation.City,
            CemeteryName = deceased.BurialLocation.CemeteryName,
            PlotNumber = deceased.BurialLocation.PlotNumber,
            GraveNumber = deceased.BurialLocation.GraveNumber,
            Accuracy = (int)deceased.BurialLocation.Accuracy,

            ShortDescription = deceased.ShortDescription,
            Biography = deceased.Biography,
            CreatedByUserId = deceased.CreatedByUserId,
            IsVerified = deceased.IsVerified,
            CreatedAtUtc = deceased.CreatedAtUtc,
            UpdatedAtUtc = deceased.UpdatedAtUtc,
            SearchKey = deceased.SearchKey,

            Metadata = new DeceasedMetadataResponse
            {
                Epitaph = deceased.Metadata.Epitaph,
                Religion = deceased.Metadata.Religion,
                Source = deceased.Metadata.Source,
                IsMilitaryService = deceased.Metadata.IsMilitaryService,
                AdditionalInfo = deceased.Metadata.AdditionalInfo
            },

            Photos = deceased.Photos.Select(p => new DeceasedPhotoResponse
            {
                Id = p.Id,
                Url = p.Url,
                Description = p.Description,
                IsPrimary = p.IsPrimary,
                CreatedAtUtc = p.CreatedAtUtc,
                AddedByUserId = p.AddedByUserId,
                ModerationStatus = (int)p.ModerationStatus
            }).ToList(),

            Memories = deceased.Memories.Select(m => new DeceasedMemoryResponse
            {
                Id = m.Id,
                Text = m.Text,
                AuthorDisplayName = m.AuthorDisplayName,
                AuthorUserId = m.AuthorUserId,
                CreatedAtUtc = m.CreatedAtUtc,
                ModerationStatus = (int)m.ModerationStatus
            }).ToList()
        };

        return Result.Success<DeceasedDetailsResponse, Error>(response);
    }
}