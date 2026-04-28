using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddPhoto.UseCase;

public sealed class AddPhotoUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IAddPhotoUseCase
{
    public Task<Result<AddPhotoResponse, Error>> Execute(
        AddPhotoCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<AddPhotoResponse, Error>> Handle(
        AddPhotoCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();

        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        if (!isAdmin && deceased.CreatedByUserId != currentUserId)
            return Errors.DeceasedPhoto.AddPhotoForbidden();

        var photoResult = deceased.AddPhoto(
            command.Url,
            currentUserId,
            command.Description,
            command.IsPrimary);

        if (photoResult.IsFailure)
            return photoResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<AddPhotoResponse, Error>(
            new AddPhotoResponse(photoResult.Value.Id));
    }
}