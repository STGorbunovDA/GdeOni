using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.UpdateTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateTracking.UseCase;

public sealed class UpdateTrackingUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateTrackingUseCase
{
    public Task<Result<UpdateTrackingResponse, Error>> Execute(
        UpdateTrackingCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UpdateTrackingResponse, Error>> Handle(
        UpdateTrackingCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;

        var user = await userRepository.GetByIdWithTracking(currentUserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserId);

        var currentStatusResult = user.GetTrackingStatus(command.DeceasedId);
        if (currentStatusResult.IsFailure)
            return currentStatusResult.Error;

        var updateResult = user.UpdateTracking(
            command.DeceasedId,
            command.RelationshipType,
            command.PersonalNotes,
            command.NotifyOnDeathAnniversary,
            command.NotifyOnBirthAnniversary);

        if (updateResult.IsFailure)
            return updateResult.Error;

        if (currentStatusResult.Value != command.TrackStatus)
        {
            var statusResult = user.ChangeTrackingStatus(command.DeceasedId, command.TrackStatus);
            if (statusResult.IsFailure)
                return statusResult.Error;
        }

        await userRepository.Save(cancellationToken);

        return Result.Success<UpdateTrackingResponse, Error>(
            new UpdateTrackingResponse(command.DeceasedId));
    }
}