using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;

public sealed class GetDeceasedByIdUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetDeceasedByIdUseCase
{
    public Task<Result<GetDeceasedByIdResult, Error>> Execute(
        GetDeceasedByIdQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetDeceasedByIdResult, Error>> Handle(
        GetDeceasedByIdQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D8.10: query-use-case → AsNoTracking-вариант.
        var deceased = await deceasedRepository.GetByIdWithMemoriesReadOnly(query.Id, cancellationToken);

        if (deceased is null)
            return Errors.General.NotFound("deceased", query.Id);

        var canSeeAllMemories = currentUserService.IsAdmin()
            || deceased.CreatedByUserId == currentUserIdResult.Value;

        return Result.Success<GetDeceasedByIdResult, Error>(
            new GetDeceasedByIdResult(deceased, canSeeAllMemories));
    }
}
