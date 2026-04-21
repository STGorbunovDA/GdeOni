using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.UseCase;

public interface IUpdatePhotoUseCase
{
    Task<Result<UpdatePhotoResponse, Error>> Execute(
        UpdatePhotoCommand command,
        CancellationToken cancellationToken);
}