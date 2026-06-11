using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.UseCase;

public interface IUpdateBurialLocationByEditorUseCase
{
    Task<UnitResult<Error>> Execute(
        UpdateBurialLocationByEditorCommand command,
        CancellationToken cancellationToken);
}
