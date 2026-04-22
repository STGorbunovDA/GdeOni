using GdeOni.API.Models.DeceasedRecords;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.Create.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.Model;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;
using GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.Model;

namespace GdeOni.API.Mappers;

public static class DeceasedRecordsMapping
{
    public static CreateDeceasedCommand ToCreateCommand(
        this CreateDeceasedRequest request,
        Guid currentUserId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.BurialLocation);

        return new CreateDeceasedCommand(
            FirstName: request.FirstName,
            LastName: request.LastName,
            MiddleName: request.MiddleName,
            BirthDate: request.BirthDate,
            DeathDate: request.DeathDate,
            ShortDescription: request.ShortDescription,
            Biography: request.Biography,
            CreatedByUserId: currentUserId,
            BurialLocation: request.BurialLocation.ToCommand(),
            Photos: request.Photos?.Select(x => x.ToCommand(currentUserId)).ToArray(),
            Memories: request.Memories?.Select(x => x.ToCommand(currentUserId)).ToArray(),
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
            Accuracy: request.Accuracy);
    }

    private static CreateDeceasedPhotoCommand ToCommand(
        this CreateDeceasedPhotoRequest request,
        Guid currentUserId)
    {
        return new CreateDeceasedPhotoCommand(
            Url: request.Url,
            Description: request.Description,
            IsPrimary: request.IsPrimary,
            AddedByUserId: currentUserId);
    }

    private static CreateDeceasedMemoryCommand ToCommand(
        this CreateDeceasedMemoryRequest request,
        Guid currentUserId)
    {
        return new CreateDeceasedMemoryCommand(
            Text: request.Text,
            AuthorUserId: currentUserId);
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
    
    public static UpdateDeceasedCommand ToCommand(
        this UpdateDeceasedRequest request,
        Guid deceasedId,
        Guid userId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.BurialLocation);

        return new UpdateDeceasedCommand(
            deceasedId,
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.BirthDate,
            request.DeathDate,
            request.ShortDescription,
            request.Biography,
            request.BurialLocation.ToCommand(),
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
            request.Accuracy);
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
    
    public static AddPhotoCommand ToCommand(
        this AddPhotoRequest request,
        Guid deceasedId,
        Guid currentUserId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AddPhotoCommand(
            deceasedId,
            request.Url,
            request.Description,
            request.IsPrimary,
            currentUserId);
    }
    
    public static UpdatePhotoCommand ToCommand(
        this UpdatePhotoRequest request,
        Guid deceasedId,
        Guid photoId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdatePhotoCommand(
            deceasedId,
            photoId,
            request.Url,
            request.Description);
    }
    
    public static AddMemoryCommand ToCommand(
        this AddMemoryRequest request,
        Guid deceasedId,
        Guid currentUserId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AddMemoryCommand(
            deceasedId,
            request.Text,
            currentUserId);
    }
    
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
    
    public static ClearMetadataCommand ToClearMetadataCommand(Guid deceasedId)
        => new(deceasedId);
    
    public static RejectMemoryCommand ToRejectMemoryCommand(Guid deceasedId, Guid memoryId)
        => new(deceasedId, memoryId);
    
    public static SetPrimaryPhotoCommand ToCommand(Guid deceasedId, Guid photoId)
        => new(deceasedId, photoId);
    
    public static ApproveMemoryCommand ToApproveMemoryCommand(Guid deceasedId, Guid memoryId)
        => new(deceasedId, memoryId);
    
    public static ApprovePhotoCommand ToApprovePhotoCommand(Guid deceasedId, Guid photoId)
        => new(deceasedId, photoId);
    
    public static RejectPhotoCommand ToRejectPhotoCommand(Guid deceasedId, Guid photoId)
        => new(deceasedId, photoId);
}