using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.UpdatePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.UpdatePhoto.UseCase;

public sealed class UpdatePhotoUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdatePhotoUseCase
{
    public Task<Result<UpdatePhotoResponse, Error>> Execute(
        UpdatePhotoRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<UpdatePhotoResponse, Error>> Handle(
        UpdatePhotoRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        var updateUrlResult = deceased.UpdatePhotoUrl(request.PhotoId, request.Url);
        if (updateUrlResult.IsFailure)
            return updateUrlResult.Error;

        var updateDescriptionResult = deceased.UpdatePhotoDescription(request.PhotoId, request.Description);
        if (updateDescriptionResult.IsFailure)
            return updateDescriptionResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UpdatePhotoResponse, Error>(
            new UpdatePhotoResponse(request.PhotoId));
    }
}