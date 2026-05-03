using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.UseCase;

public interface ISetMainMediaPhotoUseCase
{
    Task<UnitResult<Error>> Execute(
        SetMainMediaPhotoCommand command,
        CancellationToken cancellationToken);
}
