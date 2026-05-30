using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Common.Security;

/// <summary>
/// D24. Проверка права редактирования карточки умершего.
/// Правят:
///   - админы (SuperAdmin / Admin) — любую карточку;
///   - юзеры с активным трекингом (Status != Archived) — карточки,
///     которые они отслеживают.
///
/// Archived сознательно исключён: если юзер сам сказал "это не моё
/// больше", он не должен влиять на содержимое.
/// </summary>
public interface ICanEditDeceasedPolicy
{
    Task<UnitResult<Error>> CheckAsync(Guid deceasedId, CancellationToken cancellationToken);
}
