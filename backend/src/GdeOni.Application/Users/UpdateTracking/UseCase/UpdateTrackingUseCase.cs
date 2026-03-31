using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.UpdateTracking.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.UpdateTracking.UseCase;

public sealed class UpdateTrackingUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUpdateTrackingUseCase
{
    public Task<Result<UpdateTrackingResponse, Error>> Execute(
        UpdateTrackingRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<UpdateTrackingResponse, Error>> Handle(
        UpdateTrackingRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(request.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", request.UserId);

        var result = user.UpdateTracking(
            request.DeceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary);

        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<UpdateTrackingResponse, Error>(
            new UpdateTrackingResponse(request.DeceasedId));
    }
}