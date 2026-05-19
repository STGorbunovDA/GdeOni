namespace GdeOni.Domain.Shared;

/// <summary>
/// D16. Тарифный план подписки. Решение 2026-05-14: запускаем только
/// Monthly за 49 ₽; Yearly добавим если попросят (enum оставлен
/// расширяемым).
/// </summary>
public enum SubscriptionPlan
{
    Monthly = 1,
}
