using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddPhoto.Model;
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
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var isSuperAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString());

        if (!isSuperAdmin)
        {
            var userExists = await userRepository.ExistsById(command.AddedByUserId, cancellationToken);
            if (!userExists)
                return Errors.General.NotFound("user", command.AddedByUserId);
        }

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