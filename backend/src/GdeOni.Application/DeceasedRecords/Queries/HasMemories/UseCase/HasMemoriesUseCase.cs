using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;

public sealed class HasMemoriesUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IHasMemoriesUseCase
{
    public Task<Result<HasMemoriesResponse, Error>> Execute(
        HasMemoriesQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<HasMemoriesResponse, Error>> Handle(
        HasMemoriesQuery query,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        return Result.Success<HasMemoriesResponse, Error>(
            new HasMemoriesResponse(deceased.Id, deceased.HasMemories()));
    }
}