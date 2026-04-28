using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddMemory.UseCase;

public sealed class AddMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IAddMemoryUseCase
{
    public Task<Result<AddMemoryResponse, Error>> Execute(
        AddMemoryCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<AddMemoryResponse, Error>> Handle(
        AddMemoryCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();
        
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);
        
        if (!isAdmin && deceased.CreatedByUserId != currentUserId)
            return Errors.Deceased.AddMemoryForbidden();

        var memoryResult = deceased.AddMemory(command.Text, currentUserId);

        if (memoryResult.IsFailure)
            return memoryResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<AddMemoryResponse, Error>(
            new AddMemoryResponse(memoryResult.Value.Id));
    }
}