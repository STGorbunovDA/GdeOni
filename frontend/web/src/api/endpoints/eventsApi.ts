import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * События: справочник праздников. Зеркало backend GET /api/events/holidays
 * (Application/Events). Годовщины отслеживаемых умерших считаются на
 * клиенте из tracked-списка — здесь только праздники.
 */

export const HolidayCategories = {
  Memorial: 'Memorial',
  Orthodox: 'Orthodox',
  Muslim: 'Muslim',
  State: 'State',
} as const;

export type HolidayCategory =
  (typeof HolidayCategories)[keyof typeof HolidayCategories];

/**
 * Зеркало HolidayDto: date — ISO yyyy-MM-dd, category — имя enum, isMajor —
 * «крупный» праздник (по нему дефолтная галка напоминания «в день»).
 */
export type Holiday = {
  date: string;
  name: string;
  category: string;
  isMajor: boolean;
};

type GetHolidaysResponse = {
  holidays: Holiday[];
};

/**
 * Персональная настройка напоминания о празднике. holidayKey — имя праздника
 * (стабильный ключ), leadDays — набор «за сколько дней» (0 = в день, 1, 3, 7).
 * Пустой набор = напоминание отключено.
 */
export type HolidayReminder = {
  holidayKey: string;
  leadDays: number[];
};

type GetRemindersResponse = {
  reminders: HolidayReminder[];
};

export const eventsApi = {
  /**
   * GET /api/events/holidays?from=&to= — праздники в диапазоне
   * (ISO yyyy-MM-dd, включительно). Диапазон на бэке ограничен 366 днями.
   */
  async getHolidays(from: string, to: string): Promise<Holiday[]> {
    const res = await unwrap(
      apiClient.get<ApiEnvelope<GetHolidaysResponse>>('/api/events/holidays', {
        params: { from, to },
      }),
    );
    return res.holidays;
  },
};

export const holidayRemindersApi = {
  /** GET /api/events/holiday-reminders — явные настройки текущего юзера. */
  async getMine(): Promise<HolidayReminder[]> {
    const res = await unwrap(
      apiClient.get<ApiEnvelope<GetRemindersResponse>>(
        '/api/events/holiday-reminders',
      ),
    );
    return res.reminders;
  },

  /**
   * PUT /api/events/holiday-reminders — задать/обновить напоминание.
   * Пустой leadDays отключает напоминание о празднике.
   */
  async set(holidayKey: string, leadDays: number[]): Promise<HolidayReminder> {
    return unwrap(
      apiClient.put<ApiEnvelope<HolidayReminder>>(
        '/api/events/holiday-reminders',
        { holidayKey, leadDays },
      ),
    );
  },
};
