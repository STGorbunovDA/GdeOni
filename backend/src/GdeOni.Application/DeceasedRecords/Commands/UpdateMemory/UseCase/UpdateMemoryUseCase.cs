using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.UseCase;

public sealed class UpdateMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
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
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();

        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var memory = deceased.Memories.FirstOrDefault(m => m.Id == command.MemoryId);
        if (memory is null)
            return Errors.DeceasedMemory.NotFound(command.MemoryId);

        // Редактировать может: автор воспоминания, автор карточки
        // (модерация), admin. После редактирования D7.22 вернёт
        // запись в Pending — текст пройдёт модерацию заново.
        var canEdit = isAdmin
            || memory.AuthorUserId == currentUserId
            || deceased.CreatedByUserId == currentUserId;
        if (!canEdit)
            return Errors.Deceased.UpdateMemoryForbidden();

        var editTextResult = deceased.EditMemory(command.MemoryId, command.Text);
        if (editTextResult.IsFailure)
            return editTextResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UpdateMemoryResponse, Error>(
            new UpdateMemoryResponse(command.MemoryId));
    }
}