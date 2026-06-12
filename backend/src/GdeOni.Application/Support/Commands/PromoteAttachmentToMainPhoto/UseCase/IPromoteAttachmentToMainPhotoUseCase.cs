using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.PromoteAttachmentToMainPhoto.UseCase;

public interface IPromoteAttachmentToMainPhotoUseCase
{
    Task<Result<PromoteAttachmentToMainPhotoResponse, Error>> Execute(
        PromoteAttachmentToMainPhotoCommand command,
        CancellationToken cancellationToken);
}
