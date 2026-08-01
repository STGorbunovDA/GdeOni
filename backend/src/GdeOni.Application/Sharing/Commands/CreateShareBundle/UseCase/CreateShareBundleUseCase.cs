using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Sharing.Commands.CreateShareBundle.Model;
using GdeOni.Domain.Aggregates.Sharing;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Sharing.Commands.CreateShareBundle.UseCase;

/// <summary>
/// D46. Создаёт подборку карточек и короткий код к ней. Существование
/// карточек тут НЕ проверяем: id приходят из «Отслеживаемых» отправителя,
/// а на открытии/импорте несуществующие всё равно отфильтруются.
/// </summary>
public sealed class CreateShareBundleUseCase(
    IShareBundleRepository shareBundleRepository,
    IShareCodeFactory shareCodeFactory,
    ICurrentUserService currentUserService,
    IOptions<SharingOptions> sharingOptions,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : ICreateShareBundleUseCase
{
    // Предгенерация кода с проверкой на коллизию. Практически коллизий не
    // бывает (12 base62 ≈ 71 бит), но unique-индекс + пара попыток закрывают
    // её начисто.
    private const int MaxCodeAttempts = 5;

    public Task<Result<CreateShareBundleResponse, Error>> Execute(
        CreateShareBundleCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<CreateShareBundleResponse, Error>> Handle(
        CreateShareBundleCommand command,
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromHours(sharingOptions.Value.LinkLifetimeHours);

        string? code = null;
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var candidate = shareCodeFactory.Generate();
            if (!await shareBundleRepository.ExistsByCode(candidate, cancellationToken))
            {
                code = candidate;
                break;
            }
        }

        if (code is null)
            return Errors.Share.CodeGenerationFailed();

        var bundleResult = ShareBundle.Create(
            code,
            userIdResult.Value,
            command.DeceasedIds,
            nowUtc,
            lifetime);

        if (bundleResult.IsFailure)
            return bundleResult.Error;

        await shareBundleRepository.Add(bundleResult.Value, cancellationToken);
        await shareBundleRepository.Save(cancellationToken);

        return Result.Success<CreateShareBundleResponse, Error>(
            new CreateShareBundleResponse(code, bundleResult.Value.ExpiresAtUtc));
    }
}
