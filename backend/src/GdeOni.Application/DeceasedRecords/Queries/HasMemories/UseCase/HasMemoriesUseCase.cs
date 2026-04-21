using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;

public sealed class HasMemoriesUseCase(IDeceasedRepository deceasedRepository)
    : IHasMemoriesUseCase
{
    public async Task<Result<HasMemoriesResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(deceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", deceasedId);

        return Result.Success<HasMemoriesResponse, Error>(
            new HasMemoriesResponse(deceased.Id, deceased.HasMemories()));
    }
}