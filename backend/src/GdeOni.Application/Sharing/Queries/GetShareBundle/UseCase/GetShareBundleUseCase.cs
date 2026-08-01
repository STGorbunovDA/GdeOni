using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Sharing.Queries.GetShareBundle.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Sharing.Queries.GetShareBundle.UseCase;

/// <summary>
/// D46. Раскрывает подборку по коду: проверяет срок жизни и отдаёт строки
/// существующих карточек (ФИО/даты/место) в порядке подборки. Получатель
/// уже вошёл — ошибку «истекла» показываем честно (enumeration не грозит,
/// код не подбирается).
/// </summary>
public sealed class GetShareBundleUseCase(
    IShareBundleRepository shareBundleRepository,
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IGetShareBundleUseCase
{
    public Task<Result<GetShareBundleResponse, Error>> Execute(
        GetShareBundleQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetShareBundleResponse, Error>> Handle(
        GetShareBundleQuery query,
        CancellationToken cancellationToken)
    {
        var bundle = await shareBundleRepository.GetByCode(query.Code, cancellationToken);
        if (bundle is null)
            return Errors.Share.NotFound();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (bundle.IsExpired(nowUtc))
            return Errors.Share.Expired();

        var deceased = await deceasedRepository.GetForShare(bundle.DeceasedIds, cancellationToken);
        var byId = deceased.ToDictionary(d => d.Id);

        var items = new List<ShareBundleItemResponse>(bundle.DeceasedIds.Length);
        foreach (var id in bundle.DeceasedIds)
        {
            if (!byId.TryGetValue(id, out var d))
                continue; // карточку удалили между шэром и открытием — пропускаем

            items.Add(new ShareBundleItemResponse(
                d.Id,
                d.Name.FullName,
                d.LifePeriod.BirthDate,
                d.LifePeriod.DeathDate,
                d.BurialLocation?.Country,
                d.BurialLocation?.City,
                d.BurialLocation?.CemeteryName));
        }

        return Result.Success<GetShareBundleResponse, Error>(
            new GetShareBundleResponse(items, bundle.ExpiresAtUtc));
    }
}
