using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.HasMemories.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.HasMemories.UseCase;

public interface IHasMemoriesUseCase
{
    Task<Result<HasMemoriesResponse, Error>> Execute(
        Guid deceasedId,
        CancellationToken cancellationToken);
}