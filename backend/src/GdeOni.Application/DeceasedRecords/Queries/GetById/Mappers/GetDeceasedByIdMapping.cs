using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.Mappers;

public static class GetDeceasedByIdMapping
{
    public static DeceasedDetailsResponse ToResponse(this Deceased deceased)
    {
        return new DeceasedDetailsResponse
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

            Photos = deceased.Photos
                .Select(photo => new DeceasedPhotoResponse
                {
                    Id = photo.Id,
                    Url = photo.Url,
                    Description = photo.Description,
                    IsPrimary = photo.IsPrimary,
                    CreatedAtUtc = photo.CreatedAtUtc,
                    AddedByUserId = photo.AddedByUserId,
                    ModerationStatus = (int)photo.ModerationStatus
                })
                .ToArray(),

            Memories = deceased.Memories
                .Select(memory => new DeceasedMemoryResponse
                {
                    Id = memory.Id,
                    Text = memory.Text,
                    AuthorUserId = memory.AuthorUserId,
                    CreatedAtUtc = memory.CreatedAtUtc,
                    ModerationStatus = (int)memory.ModerationStatus
                })
                .ToArray()
        };
    }
}