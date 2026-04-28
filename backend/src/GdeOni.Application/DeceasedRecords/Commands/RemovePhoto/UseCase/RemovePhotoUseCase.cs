using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.UseCase;

public sealed class RemovePhotoUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRemovePhotoUseCase
{
    public Task<Result<RemovePhotoResponse, Error>> Execute(
        RemovePhotoCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<RemovePhotoResponse, Error>> Handle(
        RemovePhotoCommand command,
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
            return Errors.DeceasedPhoto.DeletePhotoForbidden();

        var result = deceased.RemovePhoto(command.PhotoId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return Result.Success<RemovePhotoResponse, Error>(
            new RemovePhotoResponse(true));
    }
}