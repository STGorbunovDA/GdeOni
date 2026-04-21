using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.HasPhotos.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.HasPhotos.UseCase;

public sealed class HasPhotosUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IHasPhotosUseCase
{
    public Task<Result<HasPhotosResponse, Error>> Execute(
        HasPhotosQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<HasPhotosResponse, Error>> Handle(
        HasPhotosQuery query,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        return Result.Success<HasPhotosResponse, Error>(
            new HasPhotosResponse(deceased.Id, deceased.HasPhotos()));
    }
}