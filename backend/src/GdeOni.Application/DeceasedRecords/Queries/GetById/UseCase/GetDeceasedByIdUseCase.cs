using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Mappers;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;

public sealed class GetDeceasedByIdUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetDeceasedByIdUseCase
{
    public Task<Result<DeceasedDetailsResponse, Error>> Execute(
        GetDeceasedByIdQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<DeceasedDetailsResponse, Error>> Handle(
        GetDeceasedByIdQuery query,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(query.Id, cancellationToken);
        
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.Id);

        return Result.Success<DeceasedDetailsResponse, Error>(deceased.ToResponse());
    }
}