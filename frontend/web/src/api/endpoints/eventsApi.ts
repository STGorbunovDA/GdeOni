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

/** Зеркало HolidayDto: date — ISO yyyy-MM-dd, category — имя enum. */
export type Holiday = {
  date: string;
  name: string;
  category: string;
};

type GetHolidaysResponse = {
  holidays: Holiday[];
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
