using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.UseCase;

public interface IUpdateMetadataByEditorUseCase
{
    Task<UnitResult<Error>> Execute(
        UpdateMetadataByEditorCommand command,
        CancellationToken cancellationToken);
}
