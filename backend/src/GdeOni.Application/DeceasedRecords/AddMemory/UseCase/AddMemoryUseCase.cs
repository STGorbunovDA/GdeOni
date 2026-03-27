using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.AddMemory.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.AddMemory.UseCase;

public sealed class AddMemoryUseCase(
    IDeceasedRepository deceasedRepository,
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IAddMemoryUseCase
{
    public Task<Result<AddMemoryResponse, Error>> Execute(
        AddMemoryRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<AddMemoryResponse, Error>> Handle(
        AddMemoryRequest request,
        CancellationToken cancellationToken)
    {
        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        if (request.AuthorUserId.HasValue)
        {
            var userExists = await userRepository.ExistsById(request.AuthorUserId.Value, cancellationToken);
            if (!userExists)
                return Errors.General.NotFound("user", request.AuthorUserId.Value);
        }

        var memoryResult = deceased.AddMemory(
            request.Text,
            request.AuthorDisplayName,
            request.AuthorUserId);

        if (memoryResult.IsFailure)
            return memoryResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<AddMemoryResponse, Error>(
            new AddMemoryResponse(memoryResult.Value.Id));
    }
}