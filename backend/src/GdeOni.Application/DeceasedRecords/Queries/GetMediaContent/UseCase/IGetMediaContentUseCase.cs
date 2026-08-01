using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaContent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaContent.UseCase;

public interface IGetMediaContentUseCase
{
    Task<Result<GetMediaContentResult, Error>> Execute(
        GetMediaContentQuery query,
        CancellationToken cancellationToken);
}
