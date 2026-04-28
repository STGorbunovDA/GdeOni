using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.UseCase;

public interface IRemovePhotoUseCase
{
    Task<Result<RemovePhotoResponse, Error>> Execute(
        RemovePhotoCommand command,
        CancellationToken cancellationToken);
}