using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.ApproveMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.ApproveMemory.UseCase;

public sealed class ApproveMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IApproveMemoryUseCase
{
    public Task<Result<ApproveMemoryResponse, Error>> Execute(
        ApproveMemoryRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<ApproveMemoryResponse, Error>> Handle(
        ApproveMemoryRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        var result = deceased.ApproveMemory(request.MemoryId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<ApproveMemoryResponse, Error>(
            new ApproveMemoryResponse(request.MemoryId));
    }
}