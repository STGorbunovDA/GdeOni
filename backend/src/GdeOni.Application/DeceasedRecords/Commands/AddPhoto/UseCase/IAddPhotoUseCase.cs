using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddPhoto.UseCase;

public interface IAddPhotoUseCase
{
    Task<Result<AddPhotoResponse, Error>> Execute(
        AddPhotoCommand command,
        CancellationToken cancellationToken);
}