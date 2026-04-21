using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;

public sealed class DeleteDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IDeleteDeceasedUseCase
{
    public async Task<UnitResult<Error>> Execute(
        DeleteDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validatedUseCaseExecutor.Execute(
            command,
            Handle,
            cancellationToken);

        return validationResult.IsFailure
            ? UnitResult.Failure(validationResult.Error)
            : UnitResult.Success<Error>();
    }

    private async Task<Result<bool, Error>> Handle(
        DeleteDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(command.Id, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.Id);

        deceasedRepository.Delete(deceased);
        await deceasedRepository.Save(cancellationToken);

        return true;
    }
}