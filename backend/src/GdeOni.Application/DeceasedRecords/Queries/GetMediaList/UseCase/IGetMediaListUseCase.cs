using CSharpFunctionalExtensions;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetMediaList.UseCase;

public interface IGetMediaListUseCase
{
    Task<Result<PagedResponse<MediaListItemResponse>, Error>> Execute(
        GetMediaListQuery query,
        CancellationToken cancellationToken);
}
