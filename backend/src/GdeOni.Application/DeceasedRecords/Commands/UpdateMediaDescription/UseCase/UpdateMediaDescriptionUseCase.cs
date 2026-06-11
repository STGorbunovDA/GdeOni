using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.UseCase;

public sealed class UpdateMediaDescriptionUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateMediaDescriptionUseCase
{
    public Task<Result<UpdateMediaDescriptionResponse, Error>> Execute(
        UpdateMediaDescriptionCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UpdateMediaDescriptionResponse, Error>> Handle(
        UpdateMediaDescriptionCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // Filtered Include — Deceased + ОДНА media (D7.47).
        var deceased = await deceasedRepository.GetByIdWithMediaById(
            command.DeceasedId, command.MediaId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var media = deceased.Media.FirstOrDefault(m => m.Id == command.MediaId);
        if (media is null)
            return Errors.DeceasedMedia.NotFound(command.MediaId);

        // D26. Редактирование описания медиа — только админ. Контроллер уже
        // ограничен Roles=SuperAdmin,Admin; этот guard — вторая линия.
        if (!currentUserService.IsAdmin())
            return Errors.DeceasedMedia.UpdateDescriptionForbidden();

        var updateResult = deceased.UpdateMediaDescription(command.MediaId, command.Description);
        if (updateResult.IsFailure)
            return updateResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UpdateMediaDescriptionResponse, Error>(
            new UpdateMediaDescriptionResponse(command.DeceasedId, command.MediaId));
    }
}
