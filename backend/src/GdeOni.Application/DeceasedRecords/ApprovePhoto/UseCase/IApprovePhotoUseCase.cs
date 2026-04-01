using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.ApprovePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.ApprovePhoto.UseCase;

public interface IApprovePhotoUseCase
{
    Task<Result<ApprovePhotoResponse, Error>> Execute(
        ApprovePhotoRequest request,
        CancellationToken cancellationToken);
}
