using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.AddPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.AddPhoto.UseCase;

public sealed class AddPhotoUseCase(
    IDeceasedRepository deceasedRepository,
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IAddPhotoUseCase
{
    public Task<Result<AddPhotoResponse, Error>> Execute(
        AddPhotoRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<AddPhotoResponse, Error>> Handle(
        AddPhotoRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        var userExists = await userRepository.ExistsById(request.AddedByUserId, cancellationToken);
        if (!userExists)
            return Errors.General.NotFound("user", request.AddedByUserId);

        var photoResult = deceased.AddPhoto(
            request.Url,
            request.AddedByUserId,
            request.Description,
            request.IsPrimary);

        if (photoResult.IsFailure)
            return photoResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<AddPhotoResponse, Error>(
            new AddPhotoResponse(photoResult.Value.Id));
    }
}