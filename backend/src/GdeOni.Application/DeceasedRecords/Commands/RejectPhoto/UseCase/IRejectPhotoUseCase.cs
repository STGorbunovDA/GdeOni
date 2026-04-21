using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.UseCase;

public interface IRejectPhotoUseCase
{
    Task<Result<RejectPhotoResponse, Error>> Execute(
        RejectPhotoCommand command,
        CancellationToken cancellationToken);
}