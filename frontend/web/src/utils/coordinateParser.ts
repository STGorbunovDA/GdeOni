/**
 * F5. Парсер координат для ручного ввода. Зеркало
 * GdeOni.Mobile.Shared/Utils/CoordinateParser.cs.
 *
 * Принимает запятую и точку как десятичный разделитель (юзер
 * копирует из карт Google, Yandex, WhatsApp, etc.), trim'ит пробелы,
 * проверяет диапазоны.
 *
 * Будет переиспользован в F8 (создание у могилы) и F15 (правка
 * координат). Пока живёт здесь, в F8 либо вытащу в /utils/, либо
 * переиспользую из этого же файла.
 */

export function tryParseDouble(input: string | null | undefined): number | null {
  if (!input) return null;
  const cleaned = input.trim().replace(',', '.');
  if (cleaned === '') return null;
  const n = Number(cleaned);
  return Number.isFinite(n) ? n : null;
}

export function tryParseLatitude(input: string | null | undefined): number | null {
  const n = tryParseDouble(input);
  if (n === null) return null;
  return n >= -90 && n <= 90 ? n : null;
}

export function tryParseLongitude(input: string | null | undefined): number | null {
  const n = tryParseDouble(input);
  if (n === null) return null;
  return n >= -180 && n <= 180 ? n : null;
}

export function tryParseAccuracy(input: string | null | undefined): number | null {
  const n = tryParseDouble(input);
  if (n === null) return null;
  return n >= 0 ? n : null;
}
