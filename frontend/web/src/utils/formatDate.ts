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

/**
 * Конверсия ISO datetime (yyyy-MM-ddTHH:mm:ss[.fff]Z) → локальный
 * формат "dd.MM.yyyy HH:mm" в текущей таймзоне браузера.
 *
 * Используется в админ-таблицах (CreatedAt, RegisteredAt и т.п.),
 * где нужны и дата, и время для аудита. В отличие от formatDateOnly
 * — здесь new Date оправдан: это полный datetime, а не date-only.
 */
export function formatDateTime(iso: string): string {
  const d = new Date(iso);
  const date = d.toLocaleDateString('ru-RU');
  const time = d.toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit',
  });
  return `${date} ${time}`;
}
