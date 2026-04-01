using CSharpFunctionalExtensions;
using GdeOni.Application.Users.HasNotificationsEnabled.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.HasNotificationsEnabled.UseCase;

public interface IHasNotificationsEnabledUseCase
{
    Task<Result<HasNotificationsEnabledResponse, Error>> Execute(
        Guid userId,
        Guid deceasedId,
        CancellationToken cancellationToken);
}