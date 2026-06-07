using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Legal.Commands.AcceptLegal.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Legal.Commands.AcceptLegal.UseCase;

/// <summary>
/// D19. Сохраняет согласие текущего пользователя с актуальными версиями
/// Privacy Policy и Terms of Use. Вызывается клиентом когда:
///  - сразу после регистрации (в Register уже фиксируем, но клиент
///    может перезапросить без потери смысла — User.AcceptLegal no-op
///    при тех же версиях);
///  - после показа модалки "Документы обновились".
///
/// Юзер берётся из JWT через <see cref="ICurrentUserService"/> — DTO
/// не содержит userId, нельзя принять согласие "за другого".
/// </summary>
public sealed class AcceptLegalUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    IOptions<LegalOptions> legalOptions,
    TimeProvider timeProvider)
    : IAcceptLegalUseCase
{
    public Task<UnitResult<Error>> Execute(
        AcceptLegalCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        AcceptLegalCommand command,
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var user = await userRepository.GetById(userIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.User.UserForbidden();

        // Клиент мог отправить версии, которые уже устарели на бэке —
        // ловим этот кейс, чтобы клиент перезагрузил актуальный текст
        // и подтвердил уже его. Иначе фиксируется "ложное" согласие
        // на устаревшую версию.
        var legal = legalOptions.Value;
        if (command.PrivacyPolicyVersion < legal.CurrentPrivacyPolicyVersion
            || command.TermsVersion < legal.CurrentTermsVersion)
        {
            return Errors.Legal.VersionOutdated();
        }

        var acceptResult = user.AcceptLegal(
            command.PrivacyPolicyVersion,
            command.TermsVersion,
            timeProvider.GetUtcNow().UtcDateTime);
        if (acceptResult.IsFailure)
            return acceptResult.Error;

        await userRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
