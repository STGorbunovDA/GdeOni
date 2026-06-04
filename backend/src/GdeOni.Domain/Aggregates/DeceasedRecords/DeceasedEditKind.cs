namespace GdeOni.Domain.Aggregates.DeceasedRecords;

/// <summary>
/// D24. Тип правки карточки умершего. Один PATCH = одна запись
/// <see cref="DeceasedEdit"/> одного Kind'а — это сознательное
/// разделение по блокам формы, не "одна запись на одно изменённое поле":
/// аудиту удобнее видеть "юзер X поменял блок 'Основное'", чем 7 строк
/// "юзер X поменял FirstName, LastName, …".
/// </summary>
public enum DeceasedEditKind
{
    MainInfo = 1,
    Metadata = 2,
    BurialLocation = 3,
    /// <summary>
    /// Админская переуступка авторства карточки — при удалении юзера
    /// SuperAdmin становится новым CreatedByUserId, а в ChangesJson
    /// фиксируется email удалённого юзера для аудита.
    /// </summary>
    Reassignment = 4,
}
