using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.AddPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.AddPhoto.UseCase;

public interface IAddPhotoUseCase
{
    Task<Result<AddPhotoResponse, Error>> Execute(
        AddPhotoRequest request,
        CancellationToken cancellationToken);
}