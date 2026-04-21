using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.HasPhotos.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.HasPhotos.UseCase;

public interface IHasPhotosUseCase
{
    Task<Result<HasPhotosResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken);
}