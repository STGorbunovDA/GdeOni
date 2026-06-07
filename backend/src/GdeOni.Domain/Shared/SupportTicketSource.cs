namespace GdeOni.Domain.Shared;

/// <summary>
/// Откуда пришёл тикет. Manual — юзер заполнил форму "Обращение в
/// поддержку". Auto — бек сам обнаружил аномалию (например, webhook
/// от YooKassa с неизвестным external_payment_id) и завёл инцидент.
/// </summary>
public enum SupportTicketSource
{
    Unknown = 0,
    Manual = 1,
    Auto = 2
}
