using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.ApprovePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.ApprovePhoto.UseCase;

public sealed class ApprovePhotoUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IApprovePhotoUseCase
{
    public Task<Result<ApprovePhotoResponse, Error>> Execute(
        ApprovePhotoRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<ApprovePhotoResponse, Error>> Handle(
        ApprovePhotoRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        var result = deceased.ApprovePhoto(request.PhotoId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<ApprovePhotoResponse, Error>(
            new ApprovePhotoResponse(request.PhotoId));
    }
}