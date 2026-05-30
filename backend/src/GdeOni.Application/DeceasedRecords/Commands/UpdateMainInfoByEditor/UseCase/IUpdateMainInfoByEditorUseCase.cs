using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.UseCase;

public interface IUpdateMainInfoByEditorUseCase
{
    Task<UnitResult<Error>> Execute(
        UpdateMainInfoByEditorCommand command,
        CancellationToken cancellationToken);
}
