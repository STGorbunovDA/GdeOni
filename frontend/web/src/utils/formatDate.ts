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
 *
 * Год дополняется нулями до 4 знаков — см. комментарий к
 * {@link parseDateInputValue} про round-trip.
 */
export function toDateInputValue(d: Date | null | undefined): string {
  if (!d) return '';
  const y = String(d.getFullYear()).padStart(4, '0');
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/**
 * Значение нативного `<input type="date">` («yyyy-MM-dd») → локальная Date.
 * Собираем через конструктор Y/M/D, чтобы не ловить UTC-сдвиг, который
 * даёт `new Date("yyyy-MM-dd")`. Пустая/битая строка → null.
 *
 * ВАЖНО ПРО ROUND-TRIP (баг «год прыгает на 1901»). Поля дат —
 * контролируемые: значение идёт `input → parse → Date → format → input`.
 * Пока человек набирает год, браузер отдаёт ПРОМЕЖУТОЧНЫЕ полные даты:
 * «1» → `0001-…`, «19» → `0019-…`, «198» → `0198-…`, «1987» → `1987-…`.
 * Значит round-trip обязан быть БЕЗ ПОТЕРЬ, иначе мы запишем в инпут не
 * то, что человек набрал, и собьём ему ввод.
 *
 * Здесь ломались два места:
 *  1. `new Date(1, 10, 11)` — конструктор трактует год 0–99 как 1900+год,
 *     то есть 1 превращался в 1901. Лечится `setFullYear`.
 *  2. `toDateInputValue` не добивал год нулями: год 1 давал «1-11-11»,
 *     что для `<input type="date">` вообще невалидно.
 * В сумме юзер набирал 1987, а получал 1901/1907 и не мог это исправить.
 */
export function parseDateInputValue(value: string): Date | null {
  const [y, m, d] = value.split('-').map(Number);
  if (!y || !m || !d) return null;

  const date = new Date(y, m - 1, d);
  // Снимает мэппинг 0–99 → 1900+год: конструктору его не отключить,
  // а setFullYear ставит год буквально.
  date.setFullYear(y);
  return date;
}
