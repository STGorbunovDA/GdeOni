using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.UseCase;

public sealed class UpdateMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateMemoryUseCase
{
    public Task<Result<UpdateMemoryResponse, Error>> Execute(
        UpdateMemoryCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UpdateMemoryResponse, Error>> Handle(
        UpdateMemoryCommand command,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var editTextResult = deceased.EditMemory(command.MemoryId, command.Text);
        if (editTextResult.IsFailure)
            return editTextResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UpdateMemoryResponse, Error>(
            new UpdateMemoryResponse(command.MemoryId));
    }
}