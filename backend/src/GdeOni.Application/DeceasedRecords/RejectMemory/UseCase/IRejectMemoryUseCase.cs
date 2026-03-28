using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.RejectMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.RejectMemory.UseCase;

public interface IRejectMemoryUseCase
{
    Task<Result<RejectMemoryResponse, Error>> Execute(
        RejectMemoryRequest request,
        CancellationToken cancellationToken);
}