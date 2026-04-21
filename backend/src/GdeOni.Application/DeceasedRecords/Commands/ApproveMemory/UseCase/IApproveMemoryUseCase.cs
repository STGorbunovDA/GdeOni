using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.UseCase;

public interface IApproveMemoryUseCase
{
    Task<Result<ApproveMemoryResponse, Error>> Execute(
        ApproveMemoryCommand command,
        CancellationToken cancellationToken);
}