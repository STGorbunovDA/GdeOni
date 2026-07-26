import type { Holiday, HolidayReminder } from '../api/endpoints/eventsApi';

/**
 * Опции напоминания (галочки в окне редактирования). days — «за сколько
 * дней»: 0 = в день, 1, 3, 7. Множественный выбор; пустой набор = отключено.
 */
export const LEAD_OPTIONS: { days: number; label: string }[] = [
  { days: 0, label: 'В день' },
  { days: 1, label: 'За день' },
  { days: 3, label: 'За 3 дня' },
  { days: 7, label: 'За неделю' },
];

/** Дефолт: крупный праздник → «в день» (0), мелкий → выключено (пусто). */
export function defaultLeadDays(isMajor: boolean): number[] {
  return isMajor ? [0] : [];
}

/** Map «ключ праздника (имя) → явно заданный набор дней». */
export function buildOverridesMap(
  reminders: HolidayReminder[],
): Map<string, number[]> {
  const map = new Map<string, number[]>();
  for (const r of reminders) map.set(r.holidayKey, r.leadDays);
  return map;
}

/** Эффективный набор: явная настройка юзера, иначе дефолт по isMajor. */
export function effectiveLeadDays(
  holiday: Holiday,
  overrides: Map<string, number[]>,
): number[] {
  return overrides.get(holiday.name) ?? defaultLeadDays(holiday.isMajor);
}

/** ISO yyyy-MM-dd + N дней (локально, без таймзонного сдвига). */
export function shiftIso(iso: string, deltaDays: number): string {
  const [y, m, d] = iso.split('-').map(Number);
  const date = new Date(y, m - 1, d + deltaDays);
  const yy = String(date.getFullYear()).padStart(4, '0');
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const dd = String(date.getDate()).padStart(2, '0');
  return `${yy}-${mm}-${dd}`;
}

export type PopupItem = { holiday: Holiday; leadDays: number };

/**
 * Что показать в попапе «при заходе»: для каждого праздника с эффективным
 * набором S — если сегодня = дата праздника − d (для d из S), это пункт
 * попапа. d = 0 → «сегодня», d > 0 → «через d дней».
 */
export function computeTodayPopupItems(
  holidays: Holiday[],
  overrides: Map<string, number[]>,
  todayIso: string,
): PopupItem[] {
  const items: PopupItem[] = [];
  for (const h of holidays) {
    for (const d of effectiveLeadDays(h, overrides)) {
      // Триггер, когда сегодня + d = дата праздника.
      if (shiftIso(todayIso, d) === h.date) {
        items.push({ holiday: h, leadDays: d });
      }
    }
  }
  // Сначала сегодняшние (d = 0), затем ближайшие.
  return items.sort((a, b) => a.leadDays - b.leadDays);
}
