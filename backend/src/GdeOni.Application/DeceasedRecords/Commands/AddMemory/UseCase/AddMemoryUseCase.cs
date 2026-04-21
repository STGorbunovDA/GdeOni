using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.Model;
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
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        if (command.AuthorUserId.HasValue)
        {
            var isSuperAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString());
            if (!isSuperAdmin)
            {
                var userExists = await userRepository.ExistsById(command.AuthorUserId.Value, cancellationToken);
                if (!userExists)
                    return Errors.General.NotFound("user", command.AuthorUserId.Value);
            }
            
        }

        var memoryResult = deceased.AddMemory(
            command.Text,
            command.AuthorUserId);

        if (memoryResult.IsFailure)
            return memoryResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<AddMemoryResponse, Error>(
            new AddMemoryResponse(memoryResult.Value.Id));
    }
}