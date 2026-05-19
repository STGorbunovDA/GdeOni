using CSharpFunctionalExtensions;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;

public interface IProcessPaymentWebhookUseCase
{
    Task<UnitResult<Error>> Execute(
        ProcessPaymentWebhookCommand command,
        CancellationToken cancellationToken);
}
