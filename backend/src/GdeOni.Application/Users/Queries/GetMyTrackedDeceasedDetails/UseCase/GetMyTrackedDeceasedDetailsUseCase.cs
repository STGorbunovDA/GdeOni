using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.UseCase;

public sealed class GetMyTrackedDeceasedDetailsUseCase(
    IUserRepository userRepository,
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetMyTrackedDeceasedDetailsUseCase
{
    public Task<Result<GetMyTrackedDeceasedDetailsResult, Error>> Execute(
        GetMyTrackedDeceasedDetailsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetMyTrackedDeceasedDetailsResult, Error>> Handle(
        GetMyTrackedDeceasedDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        // D7.55: filtered Include — грузим только tracking по DeceasedId.
        var user = await userRepository.GetByIdWithTrackingByDeceasedId(
            currentUserIdResult.Value, query.DeceasedId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        var tracking = user.GetTracking(query.DeceasedId);
        if (tracking is null || tracking.IsArchived())
            return Errors.Tracking.NotTracked();

        // D8.10: query-use-case → AsNoTracking-вариант.
        var deceased = await deceasedRepository.GetByIdWithMemoriesReadOnly(query.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", query.DeceasedId);

        var canSeeAllMemories = currentUserService.IsAdmin()
            || deceased.CreatedByUserId == currentUserIdResult.Value;

        return Result.Success<GetMyTrackedDeceasedDetailsResult, Error>(
            new GetMyTrackedDeceasedDetailsResult(deceased, tracking, canSeeAllMemories));
    }
}
