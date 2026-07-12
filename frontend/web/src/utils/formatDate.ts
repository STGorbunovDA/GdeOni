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

/**
 * Date → значение нативного `<input type="date">` в формате «yyyy-MM-dd»
 * (локальные Y/M/D, без UTC-сдвига). Для null/undefined — пустая строка.
 * Используется всеми полями дат: браузер сам рисует календарь и парсит
 * ввод, поэтому работает одинаково на Windows / Android / iOS.
 */
export function toDateInputValue(d: Date | null | undefined): string {
  if (!d) return '';
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/**
 * Значение нативного `<input type="date">` («yyyy-MM-dd») → локальная Date.
 * Собираем через конструктор Y/M/D, чтобы не ловить UTC-сдвиг, который
 * даёт `new Date("yyyy-MM-dd")`. Пустая/битая строка → null.
 */
export function parseDateInputValue(value: string): Date | null {
  const [y, m, d] = value.split('-').map(Number);
  if (!y || !m || !d) return null;
  return new Date(y, m - 1, d);
}
