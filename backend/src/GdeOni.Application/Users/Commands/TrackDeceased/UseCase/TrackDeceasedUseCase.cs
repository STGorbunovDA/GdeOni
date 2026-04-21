using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Commands.TrackDeceased.Model;
using GdeOni.Application.Users.Common;
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
        var accessError = UserAccessGuard.EnsureCanAccessUser(command.UserId, currentUserService);
        if (accessError is not null)
            return accessError;

        var user = await userRepository.GetByIdWithTracking(command.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", command.UserId);

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