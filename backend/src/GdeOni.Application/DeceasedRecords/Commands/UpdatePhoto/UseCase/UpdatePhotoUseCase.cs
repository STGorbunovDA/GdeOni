using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdatePhoto.UseCase;

public sealed class UpdatePhotoUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdatePhotoUseCase
{
    public Task<Result<UpdatePhotoResponse, Error>> Execute(
        UpdatePhotoCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UpdatePhotoResponse, Error>> Handle(
        UpdatePhotoCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
            return Errors.General.Unauthorized();
        
        var currentUserId = currentUserService.UserId.Value;
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(),
            UserRole.Admin.ToString());
        
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);
        
        if (!isAdmin && deceased.CreatedByUserId != currentUserId)
            return Errors.DeceasedPhoto.UpdatePhotoForbidden();

        var updateUrlResult = deceased.UpdatePhotoUrl(command.PhotoId, command.Url);
        if (updateUrlResult.IsFailure)
            return updateUrlResult.Error;

        var updateDescriptionResult = deceased.UpdatePhotoDescription(command.PhotoId, command.Description);
        if (updateDescriptionResult.IsFailure)
            return updateDescriptionResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UpdatePhotoResponse, Error>(
            new UpdatePhotoResponse(command.PhotoId));
    }
}