using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.UseCase;

public interface ISetPrimaryPhotoUseCase
{
    Task<Result<SetPrimaryPhotoResponse, Error>> Execute(
        SetPrimaryPhotoRequest request,
        CancellationToken cancellationToken);
}