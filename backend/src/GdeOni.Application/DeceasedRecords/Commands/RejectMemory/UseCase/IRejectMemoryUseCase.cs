using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectMemory.UseCase;

public interface IRejectMemoryUseCase
{
    Task<Result<RejectMemoryResponse, Error>> Execute(
        RejectMemoryCommand command,
        CancellationToken cancellationToken);
}