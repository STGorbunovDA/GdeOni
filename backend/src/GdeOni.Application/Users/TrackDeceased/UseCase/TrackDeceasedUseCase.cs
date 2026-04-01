using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.TrackDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.TrackDeceased.UseCase;

public sealed class TrackDeceasedUseCase(
    IUserRepository userRepository,
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ITrackDeceasedUseCase
{
    public Task<Result<TrackDeceasedResponse, Error>> Execute(
        TrackDeceasedRequest request,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(request, Handle, cancellationToken);
    }

    private async Task<Result<TrackDeceasedResponse, Error>> Handle(
        TrackDeceasedRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(request.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", request.UserId);

        var deceased = await deceasedRepository.GetById(request.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", request.DeceasedId);

        var result = user.TrackDeceased(
            request.DeceasedId,
            request.RelationshipType,
            request.PersonalNotes,
            request.NotifyOnDeathAnniversary,
            request.NotifyOnBirthAnniversary);

        if (result.IsFailure)
            return result.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<TrackDeceasedResponse, Error>(
            new TrackDeceasedResponse(request.DeceasedId));
    }
}