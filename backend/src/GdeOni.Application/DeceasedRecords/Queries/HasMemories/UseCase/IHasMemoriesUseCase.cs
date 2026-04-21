using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;

public interface IHasMemoriesUseCase
{
    Task<Result<HasMemoriesResponse, Error>> Execute(
        HasMemoriesQuery query,
        CancellationToken cancellationToken);
}