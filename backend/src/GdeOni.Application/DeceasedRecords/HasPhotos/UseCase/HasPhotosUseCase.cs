using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.HasPhotos.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.HasPhotos.UseCase;

public sealed class HasPhotosUseCase(IDeceasedRepository deceasedRepository)
    : IHasPhotosUseCase
{
    public async Task<Result<HasPhotosResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(deceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", deceasedId);

        return Result.Success<HasPhotosResponse, Error>(
            new HasPhotosResponse(deceased.Id, deceased.HasPhotos()));
    }
}