/**
 * F22. Перевод тех. имени SubscriptionPlan в UI-подпись. Сейчас
 * поддерживается только Monthly (см. Domain/Shared/SubscriptionPlan.cs
 * — enum сознательно расширяемый, но включён один тариф).
 *
 * Если пришло неизвестное значение (например, добавили Yearly на
 * бэке до релиза фронта) — возвращаем как есть, чтобы не терять
 * данные из-за рассинхрона.
 */
const DISPLAY: Record<string, string> = {
  Monthly: 'Месячный',
};

export function displaySubscriptionPlan(plan: string | null | undefined): string {
  if (!plan) return '';
  return DISPLAY[plan] ?? plan;
}
