using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.GetById.UseCase;

public interface IGetDeceasedByIdUseCase
{
    Task<Result<DeceasedDetailsResponse, Error>> Execute(Guid id, CancellationToken cancellationToken);
}