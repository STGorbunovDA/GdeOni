namespace GdeOni.Application.Users.Commands.ChangeFullName.Model;

/// <summary>
/// Смена полного имени (ФИО) в профиле. Не уникально — тёзки допустимы.
/// null/пустая строка очищает поле.
/// </summary>
public sealed record ChangeFullNameCommand(string? FullName);
