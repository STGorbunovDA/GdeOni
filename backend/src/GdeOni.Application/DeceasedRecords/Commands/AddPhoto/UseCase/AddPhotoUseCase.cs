using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddPhoto.UseCase;

public sealed class AddPhotoUseCase(
    IDeceasedRepository deceasedRepository,
    IUserRepository userRepository,
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
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return Error.Unauthorized("auth.unauthorized", "Authentication is required.");
        }

        var currentUserId = currentUserService.UserId.Value;
        var isAdmin = currentUserService.IsInRole(
            UserRole.SuperAdmin.ToString(),
            UserRole.Admin.ToString());

        if (!isAdmin && command.AddedByUserId != currentUserId)
        {
            return Error.Forbidden(
                "deceased_photo.added_by.forbidden",
                "You cannot add a photo on behalf of another user.");
        }

        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var addedByUserExists = await userRepository.ExistsById(command.AddedByUserId, cancellationToken);
        if (!addedByUserExists)
            return Errors.General.NotFound("user", command.AddedByUserId);

        var photoResult = deceased.AddPhoto(
            command.Url,
            command.AddedByUserId,
            command.Description,
            command.IsPrimary);

        if (photoResult.IsFailure)
            return photoResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<AddPhotoResponse, Error>(
            new AddPhotoResponse(photoResult.Value.Id));
    }
}