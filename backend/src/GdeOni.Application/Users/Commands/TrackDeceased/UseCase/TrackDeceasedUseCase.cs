using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.TrackDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.TrackDeceased.UseCase;

public sealed class TrackDeceasedUseCase(
    IUserRepository userRepository,
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ITrackDeceasedUseCase
{
    public Task<Result<TrackDeceasedResponse, Error>> Execute(
        TrackDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<TrackDeceasedResponse, Error>> Handle(
        TrackDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
            return Errors.General.Unauthorized();

        var currentUserId = currentUserService.UserId.Value;
        
        var user = await userRepository.GetByIdWithTracking(currentUserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserId);

        var deceasedExists = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceasedExists is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var result = user.TrackDeceased(
            command.DeceasedId,
            command.RelationshipType,
            command.PersonalNotes,
            command.NotifyOnDeathAnniversary,
            command.NotifyOnBirthAnniversary);

        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<TrackDeceasedResponse, Error>(
            new TrackDeceasedResponse(command.DeceasedId));
    }
}