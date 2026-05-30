using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.UseCase;

/// <summary>
/// D24. Любой трекающий юзер (или админ) может править основные поля
/// карточки умершего. Все изменения попадают в audit log
/// (deceased_edits). Last-write-wins — конфликтов в БД не разрешаем,
/// история всё покажет.
/// </summary>
public sealed class UpdateMainInfoByEditorUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    ICanEditDeceasedPolicy canEditPolicy,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateMainInfoByEditorUseCase
{
    public Task<UnitResult<Error>> Execute(
        UpdateMainInfoByEditorCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        UpdateMainInfoByEditorCommand command,
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

        var result = deceased.UpdateMainInfo(
            command.FirstName,
            command.LastName,
            command.MiddleName,
            command.BirthDate,
            command.DeathDate,
            command.ShortDescription,
            command.Biography,
            editorUserId: currentUserIdResult.Value);

        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
