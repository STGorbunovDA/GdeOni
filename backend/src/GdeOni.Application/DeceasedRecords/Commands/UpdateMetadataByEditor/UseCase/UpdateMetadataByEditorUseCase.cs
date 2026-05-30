using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.UseCase;

/// <summary>
/// D24. Метаданные как один атомарный блок: если все поля null/false —
/// зовём ClearMetadata() в агрегате (он сам решит no-op vs реальное
/// очищение). Иначе DeceasedMetadata.Create + UpdateMetadata. Diff
/// уйдёт в audit log одной записью DeceasedEditKind.Metadata.
/// </summary>
public sealed class UpdateMetadataByEditorUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    ICanEditDeceasedPolicy canEditPolicy,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateMetadataByEditorUseCase
{
    public Task<UnitResult<Error>> Execute(
        UpdateMetadataByEditorCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        UpdateMetadataByEditorCommand command,
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

        var editorUserId = currentUserIdResult.Value;

        // Если ВСЕ поля метаданных пустые (включая bool=false) — Clear.
        // Иначе строим metadata и UpdateMetadata. Domain сам решит no-op.
        var isEmpty = string.IsNullOrWhiteSpace(command.Epitaph) &&
                      string.IsNullOrWhiteSpace(command.Religion) &&
                      string.IsNullOrWhiteSpace(command.Source) &&
                      string.IsNullOrWhiteSpace(command.AdditionalInfo) &&
                      !command.IsMilitaryService;

        UnitResult<Error> result;
        if (isEmpty)
        {
            result = deceased.ClearMetadata(editorUserId);
        }
        else
        {
            var metadataResult = DeceasedMetadata.Create(
                command.Epitaph,
                command.Religion,
                command.Source,
                command.IsMilitaryService,
                command.AdditionalInfo);
            if (metadataResult.IsFailure)
                return metadataResult.Error;
            result = deceased.UpdateMetadata(metadataResult.Value, editorUserId);
        }

        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
