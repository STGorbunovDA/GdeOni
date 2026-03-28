using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.ApproveMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.ApproveMemory.UseCase;

public interface IApproveMemoryUseCase
{
    Task<Result<ApproveMemoryResponse, Error>> Execute(
        ApproveMemoryRequest request,
        CancellationToken cancellationToken);
}