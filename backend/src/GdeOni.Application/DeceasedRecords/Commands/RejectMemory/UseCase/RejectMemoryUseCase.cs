using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectMemory.UseCase;

public sealed class RejectMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRejectMemoryUseCase
{
    public Task<Result<RejectMemoryResponse, Error>> Execute(
        RejectMemoryCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<RejectMemoryResponse, Error>> Handle(
        RejectMemoryCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.DeceasedMemory.RejectMemoryForbidden();
        
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var result = deceased.RejectMemory(command.MemoryId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<RejectMemoryResponse, Error>(
            new RejectMemoryResponse(command.MemoryId));
    }
}