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

        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        // Любой авторизованный пользователь может оставить воспоминание —
        // это соц-фича. Контент уйдёт в Pending и попадёт публично только
        // после Approve (D7.21). Авторство фиксируется AuthorUserId.
        var memoryResult = deceased.AddMemory(command.Text, currentUserIdResult.Value);

        if (memoryResult.IsFailure)
            return memoryResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<AddMemoryResponse, Error>(
            new AddMemoryResponse(memoryResult.Value.Id));
    }
}