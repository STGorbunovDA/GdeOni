using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.RemovePhoto.UseCase;

public sealed class RemovePhotoUseCase(IDeceasedRepository deceasedRepository)
    : IRemovePhotoUseCase
{
    public async Task<UnitResult<Error>> Execute(
        Guid deceasedId,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(deceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", deceasedId);

        var result = deceased.RemovePhoto(photoId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}