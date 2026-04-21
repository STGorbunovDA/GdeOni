using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;

public sealed class RemoveMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRemoveMemoryUseCase
{
    public async Task<UnitResult<Error>> Execute(
        RemoveMemoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await validatedUseCaseExecutor.Execute(
            command,
            async (request, token) =>
            {
                var handleResult = await Handle(request, token);
                return handleResult.IsFailure
                    ? Result.Failure<bool, Error>(handleResult.Error)
                    : Result.Success<bool, Error>(true);
            },
            cancellationToken);

        return result.IsSuccess
            ? UnitResult.Success<Error>()
            : UnitResult.Failure(result.Error);
    }

    private async Task<UnitResult<Error>> Handle(
        RemoveMemoryCommand command,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var result = deceased.RemoveMemory(command.MemoryId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}