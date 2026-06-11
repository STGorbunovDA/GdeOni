namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.Model;

/// <summary>
/// D24. PATCH /api/deceased/{id}/main-info — обновление основных
/// полей карточки умершего любым трекающим юзером или админом.
/// Доменные инварианты (длины, формат имени, период жизни) проверяются
/// в Domain через value object'ы, FluentValidation отвечает только
/// за форму DTO.
/// </summary>
public sealed record UpdateMainInfoByEditorCommand(
    Guid DeceasedId,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string? ShortDescription,
    string? Biography);
