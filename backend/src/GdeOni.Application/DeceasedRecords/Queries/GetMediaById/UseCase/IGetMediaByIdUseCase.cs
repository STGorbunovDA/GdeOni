using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaById.UseCase;

public interface IGetMediaByIdUseCase
{
    Task<Result<MediaDetailsResponse, Error>> Execute(
        GetMediaByIdQuery query,
        CancellationToken cancellationToken);
}
