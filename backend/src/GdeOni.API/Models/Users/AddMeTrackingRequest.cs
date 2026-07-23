using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Users;

/// <summary>
/// Запрос добавления карточки умершего в персональный список
/// отслеживания текущего пользователя.
/// </summary>
public sealed class AddMeTrackingRequest
{
    /// <summary>Тип родственной связи пользователя с умершим.</summary>
    public RelationshipType RelationshipType { get; set; }
    /// <summary>Личные заметки пользователя об умершем.</summary>
    public string? PersonalNotes { get; set; }
    /// <summary>Получать уведомление к годовщине смерти.</summary>
    public bool NotifyOnDeathAnniversary { get; set; }
    /// <summary>Получать уведомление к годовщине рождения.</summary>
    public bool NotifyOnBirthAnniversary { get; set; }
}
