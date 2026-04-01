using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.UpdatePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.UpdatePhoto.UseCase;

public interface IUpdatePhotoUseCase
{
    Task<Result<UpdatePhotoResponse, Error>> Execute(
        UpdatePhotoRequest request,
        CancellationToken cancellationToken);
}