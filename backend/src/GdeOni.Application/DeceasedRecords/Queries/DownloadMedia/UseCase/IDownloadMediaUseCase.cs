using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.DownloadMedia.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.DownloadMedia.UseCase;

public interface IDownloadMediaUseCase
{
    Task<Result<DownloadMediaResult, Error>> Execute(
        DownloadMediaQuery query,
        CancellationToken cancellationToken);
}
