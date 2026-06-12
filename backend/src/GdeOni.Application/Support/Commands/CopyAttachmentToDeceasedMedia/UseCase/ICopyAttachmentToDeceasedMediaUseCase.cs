using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.UseCase;

public interface ICopyAttachmentToDeceasedMediaUseCase
{
    Task<Result<CopyAttachmentToDeceasedMediaResponse, Error>> Execute(
        CopyAttachmentToDeceasedMediaCommand command,
        CancellationToken cancellationToken);
}
