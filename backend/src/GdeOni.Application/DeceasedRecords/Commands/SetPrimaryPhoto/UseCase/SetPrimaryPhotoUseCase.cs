using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.SetPrimaryPhoto.UseCase;

public sealed class SetPrimaryPhotoUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ISetPrimaryPhotoUseCase
{
    public Task<Result<SetPrimaryPhotoResponse, Error>> Execute(
        SetPrimaryPhotoCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<SetPrimaryPhotoResponse, Error>> Handle(
        SetPrimaryPhotoCommand command,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var result = deceased.SetPrimaryPhoto(command.PhotoId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<SetPrimaryPhotoResponse, Error>(
            new SetPrimaryPhotoResponse(command.PhotoId));
    }
}