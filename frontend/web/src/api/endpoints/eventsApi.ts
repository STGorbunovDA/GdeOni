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

/**
 * Ручное (пользовательское) событие. date — ISO yyyy-MM-dd (якорь; повторяется
 * каждый год по дню/месяцу). leadDays — «за сколько дней» (0/1/3/7); пусто =
 * напоминание отключено. Приватное для текущего пользователя.
 */
export type CustomEvent = {
  id: string;
  title: string;
  date: string;
  leadDays: number[];
};

export const customEventsApi = {
  /** GET /api/events/custom — мои ручные события. */
  async list(): Promise<CustomEvent[]> {
    const res = await unwrap(
      apiClient.get<ApiEnvelope<{ items: CustomEvent[] }>>('/api/events/custom'),
    );
    return res.items;
  },

  /** POST /api/events/custom — создать событие. */
  async create(
    title: string,
    date: string,
    leadDays: number[],
  ): Promise<{ id: string }> {
    return unwrap(
      apiClient.post<ApiEnvelope<{ id: string }>>('/api/events/custom', {
        title,
        date,
        leadDays,
      }),
    );
  },

  /** PUT /api/events/custom/{id} — обновить событие. */
  async update(
    id: string,
    title: string,
    date: string,
    leadDays: number[],
  ): Promise<void> {
    await unwrap(
      apiClient.put<ApiEnvelope<unknown>>(`/api/events/custom/${id}`, {
        title,
        date,
        leadDays,
      }),
    );
  },

  /** DELETE /api/events/custom/{id} — удалить событие. */
  async remove(id: string): Promise<void> {
    await unwrap(
      apiClient.delete<ApiEnvelope<unknown>>(`/api/events/custom/${id}`),
    );
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
