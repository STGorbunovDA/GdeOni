using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.UseCase;

/// <summary>
/// D24. Latitude == null трактуется как "удалить координаты" → null
/// BurialLocation. Иначе строим BurialLocation.Create + ChangeBurialLocation.
/// </summary>
public sealed class UpdateBurialLocationByEditorUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    ICanEditDeceasedPolicy canEditPolicy,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateBurialLocationByEditorUseCase
{
    public Task<UnitResult<Error>> Execute(
        UpdateBurialLocationByEditorCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        UpdateBurialLocationByEditorCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var canEdit = await canEditPolicy.CheckAsync(command.DeceasedId, cancellationToken);
        if (canEdit.IsFailure)
            return canEdit.Error;

        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        BurialLocation? location = null;
        if (command.Latitude.HasValue && command.Longitude.HasValue)
        {
            var locResult = BurialLocation.Create(
                command.Latitude.Value,
                command.Longitude.Value,
                command.Country,
                command.Region,
                command.City,
                command.CemeteryName,
                command.PlotNumber,
                command.GraveNumber,
                command.Accuracy,
                command.AccuracyMeters);
            if (locResult.IsFailure)
                return locResult.Error;
            location = locResult.Value;
        }

        var result = deceased.ChangeBurialLocation(location, currentUserIdResult.Value);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
