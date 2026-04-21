using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.AddMemory.UseCase;

public sealed class AddMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    IUserRepository userRepository,
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
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return Error.Unauthorized("auth.unauthorized", "Authentication is required.");
        }

        var currentUserId = currentUserService.UserId.Value;
        var isAdmin = currentUserService.IsInRole(
            UserRole.SuperAdmin.ToString(),
            UserRole.Admin.ToString());

        if (command.AuthorUserId.HasValue && !isAdmin && command.AuthorUserId.Value != currentUserId)
        {
            return Error.Forbidden(
                "deceased_memory.author.forbidden",
                "You cannot create a memory on behalf of another user.");
        }

        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        if (command.AuthorUserId.HasValue)
        {
            var authorExists = await userRepository.ExistsById(command.AuthorUserId.Value, cancellationToken);
            if (!authorExists)
                return Errors.General.NotFound("user", command.AuthorUserId.Value);
        }

        var memoryResult = deceased.AddMemory(command.Text, command.AuthorUserId);

        if (memoryResult.IsFailure)
            return memoryResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<AddMemoryResponse, Error>(
            new AddMemoryResponse(memoryResult.Value.Id));
    }
}