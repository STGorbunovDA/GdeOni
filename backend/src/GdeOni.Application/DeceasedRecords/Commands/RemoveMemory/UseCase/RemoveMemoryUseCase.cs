using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;

public sealed class RemoveMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IRemoveMemoryUseCase
{
    public Task<Result<RemoveMemoryResponse, Error>> Execute(
        RemoveMemoryCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<RemoveMemoryResponse, Error>> Handle(
        RemoveMemoryCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();

        // D7.46: filtered Include — грузим только одну memory.
        var deceased = await deceasedRepository.GetByIdWithMemoryById(
            command.DeceasedId, command.MemoryId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var memory = deceased.Memories.FirstOrDefault(m => m.Id == command.MemoryId);
        if (memory is null)
            return Errors.DeceasedMemory.NotFound(command.MemoryId);

        // Удалять может: автор воспоминания, автор карточки (модерация),
        // admin. Прежняя логика "только автор карточки или admin"
        // блокировала автору правку собственного коммента.
        var canDelete = isAdmin
            || memory.AuthorUserId == currentUserId
            || deceased.CreatedByUserId == currentUserId;
        if (!canDelete)
            return Errors.Deceased.DeleteMemoryForbidden();

        var result = deceased.RemoveMemory(command.MemoryId);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);
        return Result.Success<RemoveMemoryResponse, Error>(
            new RemoveMemoryResponse(true));
    }
}