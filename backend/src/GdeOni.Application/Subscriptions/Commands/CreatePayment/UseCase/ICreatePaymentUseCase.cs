using CSharpFunctionalExtensions;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.CreatePayment.UseCase;

public interface ICreatePaymentUseCase
{
    Task<Result<CreatePaymentResponse, Error>> Execute(
        CreatePaymentCommand command,
        CancellationToken cancellationToken);
}
