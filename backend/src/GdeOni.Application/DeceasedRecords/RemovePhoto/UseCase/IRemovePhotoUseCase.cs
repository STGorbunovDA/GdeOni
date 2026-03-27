using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.RemovePhoto.UseCase;

public interface IRemovePhotoUseCase
{
    Task<UnitResult<Error>> Execute(Guid deceasedId, Guid photoId, CancellationToken cancellationToken);
}