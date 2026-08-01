namespace GdeOni.Application.Sharing.Commands.ImportShareBundle.Model;

/// <summary>
/// D46. Итог импорта: сколько карточек реально добавилось в активное
/// отслеживание (<paramref name="Added"/>), сколько пропущено
/// (<paramref name="Skipped"/> — уже отслеживались или карточку удалили) и
/// сколько всего было в подборке (<paramref name="Total"/>).
/// </summary>
public sealed record ImportShareBundleResponse(int Added, int Skipped, int Total);
