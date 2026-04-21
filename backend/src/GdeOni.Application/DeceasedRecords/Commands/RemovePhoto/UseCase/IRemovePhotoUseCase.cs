using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.UseCase;

public interface IRemovePhotoUseCase
{
    Task<UnitResult<Error>> Execute(Guid deceasedId, Guid photoId, CancellationToken cancellationToken);
}