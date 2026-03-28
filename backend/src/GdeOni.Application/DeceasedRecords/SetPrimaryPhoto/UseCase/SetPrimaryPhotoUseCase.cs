using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.UseCase;

public sealed class SetPrimaryPhotoUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ISetPrimaryPhotoUseCase
{
    public Task<Result<SetPrimaryPhotoResponse, Error>> Execute(
        SetPrimaryPhotoRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<SetPrimaryPhotoResponse, Error>> Handle(
        SetPrimaryPhotoRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        var result = deceased.SetPrimaryPhoto(request.PhotoId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<SetPrimaryPhotoResponse, Error>(
            new SetPrimaryPhotoResponse(request.PhotoId));
    }
}