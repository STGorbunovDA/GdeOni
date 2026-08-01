namespace GdeOni.Application.Sharing.Commands.ImportShareBundle.Model;

/// <summary>
/// D46. Импорт подборки в своё отслеживание по коду — кнопка «Добавить»
/// на экране получателя.
/// </summary>
public sealed record ImportShareBundleCommand(string Code);
