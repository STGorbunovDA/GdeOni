using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetTrackedDeceased.UseCase;

public sealed class GetTrackedDeceasedUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetTrackedDeceasedUseCase
{
    public Task<Result<GetTrackedDeceasedResponse, Error>> Execute(
        GetTrackedDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetTrackedDeceasedResponse, Error>> Handle(
        GetTrackedDeceasedQuery query,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(query.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", query.UserId);

        var items = user.TrackedDeceasedItems
            .Select(x => new TrackedDeceasedItemResponse
            {
                Id = x.Id,
                UserId = query.UserId,
                DeceasedId = x.DeceasedId,
                RelationshipType = x.RelationshipType.ToString(),
                PersonalNotes = x.PersonalNotes,
                NotifyOnDeathAnniversary = x.NotifyOnDeathAnniversary,
                NotifyOnBirthAnniversary = x.NotifyOnBirthAnniversary,
                HasNotificationsEnabled = x.HasNotificationsEnabled(),
                Status = x.Status.ToString(),
                TrackedAtUtc = x.TrackedAtUtc,
                IsActive = x.IsActive(),
                IsMuted = x.IsMuted(),
                IsArchived = x.IsArchived()
            })
            .ToList();

        return Result.Success<GetTrackedDeceasedResponse, Error>(
            new GetTrackedDeceasedResponse
            {
                Items = items
            });
    }
}