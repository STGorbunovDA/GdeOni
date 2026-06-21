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
    /// <summary>Маппит результат use case <c>GetDeceasedById</c> в response-DTO деталей.</summary>
    public static DeceasedDetailsResponse ToDetailsResponse(this GetDeceasedByIdResult result) =>
        result.Deceased.ToDetailsResponse(
            result.CanSeeAllMemories,
            result.MainMediaId,
            result.MainPhotoBucket,
            result.MainPhotoStorageKey,
            result.MainPhotoUrl,
            result.AuthorNames);

    /// <summary>Маппит результат use case <c>GetMyTrackedDeceasedDetails</c> в response-DTO с tracking-блоком.</summary>
    public static MyTrackedDeceasedDetailsResponse ToDetailsResponse(this GetMyTrackedDeceasedDetailsResult result) =>
        new()
        {
            Deceased = result.Deceased.ToDetailsResponse(
                result.CanSeeAllMemories,
                result.MainMediaId,
                result.MainPhotoBucket,
                result.MainPhotoStorageKey,
                result.MainPhotoUrl,
                result.AuthorNames),
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

    /// <summary>
    /// Маппит доменный агрегат <see cref="Deceased"/> в response-DTO с
    /// учётом видимости неподтверждённых воспоминаний и данных главного фото.
    /// </summary>
    public static DeceasedDetailsResponse ToDetailsResponse(
        this Deceased deceased,
        bool canSeeAllMemories,
        Guid? mainMediaId = null,
        string? mainPhotoBucket = null,
        string? mainPhotoStorageKey = null,
        string? mainPhotoUrl = null,
        IReadOnlyDictionary<Guid, string>? authorNames = null)
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
            Accuracy = deceased.BurialLocation?.Accuracy.ToString(),

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

            // D8.11: фиксируем порядок по CreatedAtUtc — без OrderBy
            // EF не гарантирует стабильный ordering между запросами.
            // D8.12: ModerationStatus отдаём строкой, как Tracking.Status
            // и Media.ModerationStatus, а не магическим int.
            Memories = memoriesQuery
                .OrderBy(memory => memory.CreatedAtUtc)
                .Select(memory => new DeceasedMemoryResponse
                {
                    Id = memory.Id,
                    Text = memory.Text,
                    AuthorUserId = memory.AuthorUserId,
                    AuthorName = memory.AuthorUserId is { } authorId
                        && authorNames is not null
                        && authorNames.TryGetValue(authorId, out var name)
                            ? name
                            : null,
                    CreatedAtUtc = memory.CreatedAtUtc,
                    UpdatedAtUtc = memory.UpdatedAtUtc,
                    ModerationStatus = memory.ModerationStatus.ToString()
                })
                .ToArray(),

            MainMediaId = mainMediaId,
            MainPhotoBucket = mainPhotoBucket,
            MainPhotoStorageKey = mainPhotoStorageKey,
            MainPhotoUrl = mainPhotoUrl,
        };
    }
}
