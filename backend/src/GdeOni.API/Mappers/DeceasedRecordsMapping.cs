using GdeOni.API.Models.DeceasedRecords;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.Model;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Model;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;
using GdeOni.Application.DeceasedRecords.Commands.Verify.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.Model;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Model;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;

namespace GdeOni.API.Mappers;

/// <summary>
/// Request → Command/Query маппинг для контроллеров карточки умершего.
/// Конвенция Clean Architecture: presentation-слой переводит DTO в
/// доменные команды, чтобы Application не знал про HTTP-формат.
/// </summary>
public static class DeceasedRecordsMapping
{
    /// <summary>Маппит DTO создания карточки умершего в команду use case.</summary>
    public static CreateDeceasedCommand ToCreateCommand(
        this CreateDeceasedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateDeceasedCommand(
            FirstName: request.FirstName,
            LastName: request.LastName,
            MiddleName: request.MiddleName,
            BirthDate: request.BirthDate,
            DeathDate: request.DeathDate,
            ShortDescription: request.ShortDescription,
            Biography: request.Biography,
            BurialLocation: request.BurialLocation?.ToCommand(),
            Memories: request.Memories?.Select(x => x.ToCommand()).ToArray(),
            Metadata: request.Metadata?.ToCommand());
    }

    private static CreateDeceasedBurialLocationCommand ToCommand(
        this CreateDeceasedBurialLocationRequest request)
    {
        return new CreateDeceasedBurialLocationCommand(
            Latitude: request.Latitude,
            Longitude: request.Longitude,
            Country: request.Country,
            Region: request.Region,
            City: request.City,
            CemeteryName: request.CemeteryName,
            PlotNumber: request.PlotNumber,
            GraveNumber: request.GraveNumber,
            Accuracy: request.Accuracy,
            AccuracyMeters: request.AccuracyMeters);
    }

    private static CreateDeceasedMemoryCommand ToCommand(
        this CreateDeceasedMemoryRequest request)
    {
        return new CreateDeceasedMemoryCommand(Text: request.Text);
    }

    private static CreateDeceasedMetadataCommand ToCommand(
        this CreateDeceasedMetadataRequest request)
    {
        return new CreateDeceasedMetadataCommand(
            Epitaph: request.Epitaph,
            Religion: request.Religion,
            Source: request.Source,
            IsMilitaryService: request.IsMilitaryService,
            AdditionalInfo: request.AdditionalInfo);
    }

    /// <summary>Маппит DTO обновления карточки умершего в команду use case.</summary>
    public static UpdateDeceasedCommand ToCommand(
        this UpdateDeceasedRequest request,
        Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateDeceasedCommand(
            deceasedId,
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.BirthDate,
            request.DeathDate,
            request.ShortDescription,
            request.Biography,
            request.BurialLocation?.ToCommand(),
            request.Metadata?.ToCommand());
    }

    private static UpdateDeceasedBurialLocationCommand ToCommand(
        this UpdateDeceasedBurialLocationRequest request)
    {
        return new UpdateDeceasedBurialLocationCommand(
            request.Latitude,
            request.Longitude,
            request.Country,
            request.Region,
            request.City,
            request.CemeteryName,
            request.PlotNumber,
            request.GraveNumber,
            request.Accuracy,
            request.AccuracyMeters);
    }

    private static UpdateDeceasedMetadataCommand ToCommand(
        this UpdateDeceasedMetadataRequest request)
    {
        return new UpdateDeceasedMetadataCommand(
            request.Epitaph,
            request.Religion,
            request.Source,
            request.IsMilitaryService,
            request.AdditionalInfo);
    }

    /// <summary>Маппит DTO "добавить умершего у могилы" в команду use case.</summary>
    public static AddDeceasedAtGraveCommand ToCommand(
        this AddDeceasedAtGraveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AddDeceasedAtGraveCommand(
            FirstName: request.FirstName,
            LastName: request.LastName,
            MiddleName: request.MiddleName,
            BirthDate: request.BirthDate,
            DeathDate: request.DeathDate,
            ShortDescription: request.ShortDescription,
            Biography: request.Biography,
            GraveLocation: request.GraveLocation?.ToCommand()!,
            Tracking: request.Tracking?.ToCommand()!);
    }

    private static AddDeceasedAtGraveLocationCommand ToCommand(
        this AddDeceasedAtGraveLocationRequest request)
    {
        return new AddDeceasedAtGraveLocationCommand(
            Latitude: request.Latitude,
            Longitude: request.Longitude,
            AccuracyMeters: request.AccuracyMeters,
            Country: request.Country,
            City: request.City,
            CemeteryName: request.CemeteryName,
            PlotNumber: request.PlotNumber,
            GraveNumber: request.GraveNumber);
    }

    private static AddDeceasedAtGraveTrackingCommand ToCommand(
        this AddDeceasedAtGraveTrackingRequest request)
    {
        return new AddDeceasedAtGraveTrackingCommand(
            RelationshipType: request.RelationshipType,
            PersonalNotes: request.PersonalNotes,
            NotifyOnDeathAnniversary: request.NotifyOnDeathAnniversary,
            NotifyOnBirthAnniversary: request.NotifyOnBirthAnniversary);
    }

    /// <summary>Маппит DTO установки координат из GPS в команду use case.</summary>
    public static SetBurialLocationFromGpsCommand ToCommand(
        this SetBurialLocationFromGpsRequest request,
        Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetBurialLocationFromGpsCommand(
            deceasedId,
            request.Latitude,
            request.Longitude,
            request.AccuracyMeters);
    }

    /// <summary>Маппит DTO добавления воспоминания в команду use case.</summary>
    public static AddMemoryCommand ToCommand(
        this AddMemoryRequest request,
        Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AddMemoryCommand(
            deceasedId,
            request.Text);
    }

    /// <summary>Маппит DTO правки воспоминания в команду use case.</summary>
    public static UpdateMemoryCommand ToCommand(
        this UpdateMemoryRequest request,
        Guid deceasedId,
        Guid memoryId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateMemoryCommand(
            deceasedId,
            memoryId,
            request.Text);
    }

    /// <summary>Маппит DTO обновления метаданных в команду use case.</summary>
    public static UpdateMetadataCommand ToCommand(
        this UpdateMetadataRequest request,
        Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateMetadataCommand(
            deceasedId,
            request.Epitaph,
            request.Religion,
            request.Source,
            request.IsMilitaryService,
            request.AdditionalInfo);
    }

    /// <summary>Возвращает команду очистки метаданных для карточки <paramref name="deceasedId"/>.</summary>
    public static ClearMetadataCommand ToClearMetadataCommand(Guid deceasedId)
        => new(deceasedId);

    /// <summary>Маппит DTO правки описания медиа в команду use case.</summary>
    public static UpdateMediaDescriptionCommand ToCommand(
        this UpdateMediaDescriptionRequest request,
        Guid deceasedId,
        Guid mediaId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateMediaDescriptionCommand(deceasedId, mediaId, request.Description);
    }

    /// <summary>Возвращает команду отклонения модерации воспоминания.</summary>
    public static RejectMemoryCommand ToRejectMemoryCommand(Guid deceasedId, Guid memoryId)
        => new(deceasedId, memoryId);

    /// <summary>Возвращает команду подтверждения модерации воспоминания.</summary>
    public static ApproveMemoryCommand ToApproveMemoryCommand(Guid deceasedId, Guid memoryId)
        => new(deceasedId, memoryId);

    /// <summary>Возвращает команду подтверждения модерации медиа.</summary>
    public static ApproveMediaModerationCommand ToApproveMediaModerationCommand(Guid deceasedId, Guid mediaId)
        => new(deceasedId, mediaId);

    /// <summary>Возвращает команду отклонения модерации медиа.</summary>
    public static RejectMediaModerationCommand ToRejectMediaModerationCommand(Guid deceasedId, Guid mediaId)
        => new(deceasedId, mediaId);

    /// <summary>Возвращает запрос карточки умершего по идентификатору.</summary>
    public static GetDeceasedByIdQuery ToGetByIdQuery(Guid id)
        => new(id);

    /// <summary>Возвращает команду удаления карточки умершего.</summary>
    public static DeleteDeceasedCommand ToDeleteCommand(Guid id)
        => new(id);

    /// <summary>Возвращает команду очистки места захоронения.</summary>
    public static ClearBurialLocationCommand ToClearBurialLocationCommand(Guid deceasedId)
        => new(deceasedId);

    /// <summary>Возвращает команду удаления воспоминания.</summary>
    public static RemoveMemoryCommand ToRemoveMemoryCommand(Guid deceasedId, Guid memoryId)
        => new(deceasedId, memoryId);

    /// <summary>Возвращает команду верификации карточки администратором.</summary>
    public static VerifyDeceasedCommand ToVerifyCommand(Guid deceasedId)
        => new(deceasedId);

    /// <summary>Возвращает команду снятия флага верификации с карточки.</summary>
    public static UnverifiedDeceasedCommand ToUnverifiedCommand(Guid deceasedId)
        => new(deceasedId);

    /// <summary>Маппит DTO выборки медиа-листа в запрос use case.</summary>
    public static GetMediaListQuery ToQuery(this GetMediaListRequest request, Guid deceasedId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetMediaListQuery(
            deceasedId,
            request.Kind,
            request.ModerationStatus,
            request.Page,
            request.PageSize);
    }

    /// <summary>Возвращает запрос медиа-файла по идентификатору.</summary>
    public static GetMediaByIdQuery ToGetMediaByIdQuery(Guid deceasedId, Guid mediaId)
        => new(deceasedId, mediaId);

    /// <summary>Возвращает команду удаления медиа-файла.</summary>
    public static DeleteMediaCommand ToDeleteMediaCommand(Guid deceasedId, Guid mediaId)
        => new(deceasedId, mediaId);

    /// <summary>Возвращает команду установки главного фото карточки.</summary>
    public static SetMainMediaPhotoCommand ToSetMainMediaPhotoCommand(Guid deceasedId, Guid mediaId)
        => new(deceasedId, mediaId);

    /// <summary>Возвращает запрос расчёта расстояния от точки до могилы.</summary>
    public static GetDistanceQuery ToDistanceQuery(Guid deceasedId, double latitude, double longitude)
        => new(deceasedId, latitude, longitude);

    /// <summary>Возвращает запрос вычисления возраста на момент смерти.</summary>
    public static GetAgeAtDeathQuery ToAgeAtDeathQuery(Guid deceasedId)
        => new(deceasedId);

    /// <summary>Возвращает запрос проверки наличия воспоминаний на карточке.</summary>
    public static HasMemoriesQuery ToHasMemoriesQuery(Guid deceasedId)
        => new(deceasedId);
}
