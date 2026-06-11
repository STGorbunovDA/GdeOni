using CSharpFunctionalExtensions;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.UseCase;

public interface IRevokeSubscriptionByAdminUseCase
{
    Task<UnitResult<Error>> Execute(
        RevokeSubscriptionByAdminCommand command,
        CancellationToken cancellationToken);
}
