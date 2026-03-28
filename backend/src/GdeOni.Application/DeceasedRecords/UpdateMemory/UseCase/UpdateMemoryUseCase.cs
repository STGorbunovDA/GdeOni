using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.UpdateMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.UpdateMemory.UseCase;

public sealed class UpdateMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateMemoryUseCase
{
    public Task<Result<UpdateMemoryResponse, Error>> Execute(
        UpdateMemoryRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<UpdateMemoryResponse, Error>> Handle(
        UpdateMemoryRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        var editTextResult = deceased.EditMemory(request.MemoryId, request.Text);
        if (editTextResult.IsFailure)
            return editTextResult.Error;

        var updateAuthorResult = deceased.UpdateMemoryAuthorDisplayName(
            request.MemoryId,
            request.AuthorDisplayName);

        if (updateAuthorResult.IsFailure)
            return updateAuthorResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UpdateMemoryResponse, Error>(
            new UpdateMemoryResponse(request.MemoryId));
    }
}