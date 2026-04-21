using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemovePhoto.UseCase;

public sealed class RemovePhotoUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRemovePhotoUseCase
{
    public async Task<UnitResult<Error>> Execute(
        RemovePhotoCommand command,
        CancellationToken cancellationToken)
    {
        var result = await validatedUseCaseExecutor.Execute(
            command,
            async (request, token) =>
            {
                var result = await Handle(request, token);
                return result.IsFailure
                    ? Result.Failure<bool, Error>(result.Error)
                    : Result.Success<bool, Error>(true);
            },
            cancellationToken);

        return result.IsFailure
            ? UnitResult.Failure(result.Error)
            : UnitResult.Success<Error>();
    }

    private async Task<UnitResult<Error>> Handle(
        RemovePhotoCommand command,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var result = deceased.RemovePhoto(command.PhotoId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}