/**
 * Конверсия ISO date-only (yyyy-MM-dd) → русский формат dd.MM.yyyy.
 *
 * Бэк отдаёт DateOnly строго в ISO, парсить через new Date нельзя —
 * получим Z-сдвиг по таймзоне и можем съехать на сутки. Здесь
 * просто разбиваем строку.
 */
export function formatDateOnly(iso: string): string {
  const [y, m, d] = iso.split('-');
  return `${d}.${m}.${y}`;
}
