using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Legal;
using GdeOni.Application.Subscriptions;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Users.Commands.Register.UseCase;

public sealed class RegisterUserUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    IOptions<SubscriptionOptions> subscriptionOptions,
    IOptions<LegalOptions> legalOptions,
    TimeProvider timeProvider)
    : IRegisterUserUseCase
{
    public Task<Result<RegisterUserResponse, Error>> Execute(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(
            command,
            Handle,
            cancellationToken);
    }

    private async Task<Result<RegisterUserResponse, Error>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var emailExists = await userRepository.ExistsByEmail(command.Email, cancellationToken);
        if (emailExists)
            return Errors.User.EmailAlreadyExists();

        // Единый источник истины для normalized-формы — Domain.User
        // (D11.8.3): иначе при изменении правил нормализации Application
        // и Domain могут разойтись.
        var effectiveUserName = User.ComputeNormalizedUserName(command.UserName, command.Email);

        var userNameExists = await userRepository.ExistsByUserName(effectiveUserName, cancellationToken);
        if (userNameExists)
            return Errors.User.UserNameAlreadyExists();

        var passwordHash = passwordHasher.Hash(command.Password);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var userResult = User.Register(
            command.Email,
            passwordHash,
            command.BirthDate,
            nowUtc,
            command.FullName,
            command.UserName);

        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.Value;

        // D16. Каждый новый пользователь сразу получает Trial-период
        // на длительность из SubscriptionOptions (30 дней по дефолту).
        // Решение 2026-05-14: первый месяц бесплатно. StartTrial
        // idempotent — повторный вызов на не-None ничего не сделает.
        var trialResult = user.StartTrial(
            nowUtc,
            subscriptionOptions.Value.TrialDuration);
        if (trialResult.IsFailure)
            return trialResult.Error;

        // D19. 152-ФЗ: фиксируем версии Privacy/Terms на момент
        // регистрации. Чекбоксы в DTO уже провалидированы (см.
        // RegisterUserCommandValidator); сюда мы доходим только если
        // PrivacyPolicyAccepted=true и TermsAccepted=true.
        var legal = legalOptions.Value;
        var legalResult = user.AcceptLegal(
            legal.CurrentPrivacyPolicyVersion,
            legal.CurrentTermsVersion,
            nowUtc);
        if (legalResult.IsFailure)
            return legalResult.Error;

        await userRepository.Add(user, cancellationToken);
        await userRepository.Save(cancellationToken);
        return Result.Success<RegisterUserResponse, Error>(new RegisterUserResponse(user.Id));
    }
}