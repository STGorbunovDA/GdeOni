using GdeOni.API.Models.DeceasedRecords;
using GdeOni.API.Models.Users;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.API.Mappers;

/// <summary>
/// Domain → Response маппинг для эндпоинтов карточки умершего.
/// Живёт в API: задача presentation-слоя; use case'ы возвращают
/// доменный агрегат + флаги, контроллер мапит в DTO. См. D7.62.
/// </summary>
public static class DeceasedRecordsResponseMapping
{
    public static DeceasedDetailsResponse ToDetailsResponse(this GetDeceasedByIdResult result) =>
        result.Deceased.ToDetailsResponse(result.CanSeeAllMemories);

    public static MyTrackedDeceasedDetailsResponse ToDetailsResponse(this GetMyTrackedDeceasedDetailsResult result) =>
        new()
        {
            Deceased = result.Deceased.ToDetailsResponse(result.CanSeeAllMemories),
            Tracking = new MyTrackingInfoResponse
            {
                TrackingId = result.Tracking.Id,
                RelationshipType = result.Tracking.RelationshipType.ToString(),
                PersonalNotes = result.Tracking.PersonalNotes,
                NotifyOnDeathAnniversary = result.Tracking.NotifyOnDeathAnniversary,
                NotifyOnBirthAnniversary = result.Tracking.NotifyOnBirthAnniversary,
                Status = result.Tracking.Status.ToString(),
                TrackedAtUtc = result.Tracking.TrackedAtUtc
            }
        };

    public static DeceasedDetailsResponse ToDetailsResponse(
        this Deceased deceased,
        bool canSeeAllMemories)
    {
        var memoriesQuery = canSeeAllMemories
            ? deceased.Memories
            : deceased.Memories.Where(m => m.ModerationStatus == ModerationStatus.Approved);

        return new DeceasedDetailsResponse
        {
            Id = deceased.Id,
            FirstName = deceased.Name.FirstName,
            LastName = deceased.Name.LastName,
            MiddleName = deceased.Name.MiddleName,
            FullName = deceased.Name.FullName,

            BirthDate = deceased.LifePeriod.BirthDate,
            DeathDate = deceased.LifePeriod.DeathDate,

            HasBurialLocation = deceased.BurialLocation is not null,
            Latitude = deceased.BurialLocation?.Latitude,
            Longitude = deceased.BurialLocation?.Longitude,
            AccuracyMeters = deceased.BurialLocation?.AccuracyMeters,
            Country = deceased.BurialLocation?.Country,
            Region = deceased.BurialLocation?.Region,
            City = deceased.BurialLocation?.City,
            CemeteryName = deceased.BurialLocation?.CemeteryName,
            PlotNumber = deceased.BurialLocation?.PlotNumber,
            GraveNumber = deceased.BurialLocation?.GraveNumber,
            Accuracy = deceased.BurialLocation is null ? null : (int)deceased.BurialLocation.Accuracy,

            ShortDescription = deceased.ShortDescription,
            Biography = deceased.Biography,

            CreatedByUserId = deceased.CreatedByUserId,
            IsVerified = deceased.IsVerified,
            CreatedAtUtc = deceased.CreatedAtUtc,
            UpdatedAtUtc = deceased.UpdatedAtUtc,

            Metadata = new DeceasedMetadataResponse
            {
                Epitaph = deceased.Metadata.Epitaph,
                Religion = deceased.Metadata.Religion,
                Source = deceased.Metadata.Source,
                IsMilitaryService = deceased.Metadata.IsMilitaryService,
                AdditionalInfo = deceased.Metadata.AdditionalInfo
            },

            Memories = memoriesQuery
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
