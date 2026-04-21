using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.UseCase;

public interface ISetPrimaryPhotoUseCase
{
    Task<Result<SetPrimaryPhotoResponse, Error>> Execute(
        SetPrimaryPhotoCommand command,
        CancellationToken cancellationToken);
}