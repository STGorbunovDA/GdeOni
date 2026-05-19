using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Payments;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Subscriptions.Commands.CreatePayment.UseCase;

public sealed class CreatePaymentUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IPaymentProvider paymentProvider,
    IOptions<SubscriptionOptions> subscriptionOptions,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : ICreatePaymentUseCase
{
    public Task<Result<CreatePaymentResponse, Error>> Execute(
        CreatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<CreatePaymentResponse, Error>> Handle(
        CreatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;
        var userId = currentUserIdResult.Value;

        var user = await userRepository.GetById(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var options = subscriptionOptions.Value;

        // Создаём платёж у провайдера ДО мутации User: если YooKassa
        // отдала ошибку — User остаётся с прежним Subscription,
        // никакого "висящего" PendingPayment. Save() ниже атомарно
        // фиксирует "у нас есть paymentId от провайдера".
        var paymentResult = await paymentProvider.CreateAsync(
            userId,
            options.MonthlyPriceRub,
            options.ProductDescription,
            options.ReturnUrl,
            cancellationToken);

        if (paymentResult.IsFailure)
            return paymentResult.Error;

        var payment = paymentResult.Value;

        var requestResult = user.RequestSubscriptionPayment(command.Plan, payment.ExternalPaymentId);
        if (requestResult.IsFailure)
            return requestResult.Error;

        await userRepository.Save(cancellationToken);

        return Result.Success<CreatePaymentResponse, Error>(
            new CreatePaymentResponse(payment.CheckoutUrl, payment.ExternalPaymentId));
    }
}
