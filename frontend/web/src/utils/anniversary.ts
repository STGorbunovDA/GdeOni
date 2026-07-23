/**
 * Клиентское зеркало backend-логики D37 (AnniversaryOccurrence):
 * «наступает ли сегодня годовщина ISO-даты (yyyy-MM-dd) + сколько лет».
 * 29 февраля в невисокосный год отмечаем 28-го. Годы считаем от события
 * до сегодня (≥ 1). ISO парсим разбиением строки — без new Date, чтобы
 * не поймать UTC-сдвиг.
 */

function isLeapYear(year: number): boolean {
  return (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
}

/**
 * Если сегодня — годовщина `eventIso`, возвращает число прошедших лет
 * (≥ 1), иначе null.
 */
export function anniversaryYearsToday(
  eventIso: string,
  today: Date = new Date(),
): number | null {
  const parts = eventIso.split('-').map(Number);
  const [ey, em, ed] = parts;
  if (!ey || !em || !ed) return null;

  const ty = today.getFullYear();
  const tm = today.getMonth() + 1;
  const td = today.getDate();

  const directMatch = em === tm && ed === td;
  const febMatch =
    em === 2 && ed === 29 && tm === 2 && td === 28 && !isLeapYear(ty);

  if (!directMatch && !febMatch) return null;

  const years = ty - ey;
  return years >= 1 ? years : null;
}

/** Русская форма «год/года/лет» для числа. */
export function yearsWord(count: number): string {
  const n = Math.abs(count);
  const mod100 = n % 100;
  const mod10 = n % 10;
  if (mod100 >= 11 && mod100 <= 14) return 'лет';
  if (mod10 === 1) return 'год';
  if (mod10 >= 2 && mod10 <= 4) return 'года';
  return 'лет';
}
