using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UploadMedia.UseCase;

public interface IUploadMediaUseCase
{
    Task<Result<UploadMediaResponse, Error>> Execute(
        UploadMediaCommand command,
        CancellationToken cancellationToken);
}
