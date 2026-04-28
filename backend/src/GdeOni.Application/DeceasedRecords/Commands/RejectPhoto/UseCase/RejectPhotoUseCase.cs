using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectPhoto.UseCase;

public sealed class RejectPhotoUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRejectPhotoUseCase
{
    public Task<Result<RejectPhotoResponse, Error>> Execute(
        RejectPhotoCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<RejectPhotoResponse, Error>> Handle(
        RejectPhotoCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.DeceasedPhoto.RejectPhotoForbidden();
        
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var result = deceased.RejectPhoto(command.PhotoId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<RejectPhotoResponse, Error>(
            new RejectPhotoResponse(command.PhotoId));
    }
}