using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.RejectPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.RejectPhoto.UseCase;

public interface IRejectPhotoUseCase
{
    Task<Result<RejectPhotoResponse, Error>> Execute(
        RejectPhotoRequest request,
        CancellationToken cancellationToken);
}