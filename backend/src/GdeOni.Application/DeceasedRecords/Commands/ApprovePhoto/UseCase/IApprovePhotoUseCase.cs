using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ApprovePhoto.UseCase;

public interface IApprovePhotoUseCase
{
    Task<Result<ApprovePhotoResponse, Error>> Execute(
        ApprovePhotoCommand command,
        CancellationToken cancellationToken);
}