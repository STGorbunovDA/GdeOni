namespace GdeOni.Application.Sharing.Commands.CreateShareBundle.Model;

/// <summary>
/// D46. Создание подборки для «поделиться»: список id выбранных карточек
/// (отправитель отмечает их галочками в «Отслеживаемых»).
/// </summary>
public sealed record CreateShareBundleCommand(IReadOnlyList<Guid> DeceasedIds);
