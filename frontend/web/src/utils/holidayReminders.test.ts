import { describe, it, expect } from 'vitest';
import type { Holiday } from '../api/endpoints/eventsApi';
import {
  buildOverridesMap,
  computeTodayPopupItems,
  defaultLeadDays,
  effectiveLeadDays,
  shiftIso,
} from './holidayReminders';

function holiday(name: string, date: string, isMajor: boolean): Holiday {
  return { name, date, category: 'Orthodox', isMajor };
}

describe('holidayReminders', () => {
  describe('defaultLeadDays', () => {
    it('крупный → [0] (в день), мелкий → []', () => {
      expect(defaultLeadDays(true)).toEqual([0]);
      expect(defaultLeadDays(false)).toEqual([]);
    });
  });

  describe('effectiveLeadDays', () => {
    it('берёт явную настройку юзера, если она есть', () => {
      const h = holiday('Пасха', '2026-04-12', true);
      const overrides = buildOverridesMap([
        { holidayKey: 'Пасха', leadDays: [3, 7] },
      ]);
      expect(effectiveLeadDays(h, overrides)).toEqual([3, 7]);
    });

    it('иначе — дефолт по isMajor', () => {
      const major = holiday('Рождество', '2026-01-07', true);
      const minor = holiday('Ильин день', '2026-08-02', false);
      const empty = new Map<string, number[]>();
      expect(effectiveLeadDays(major, empty)).toEqual([0]);
      expect(effectiveLeadDays(minor, empty)).toEqual([]);
    });

    it('явное отключение (пустой набор) перебивает дефолт крупного', () => {
      const major = holiday('Рождество', '2026-01-07', true);
      const overrides = buildOverridesMap([
        { holidayKey: 'Рождество', leadDays: [] },
      ]);
      expect(effectiveLeadDays(major, overrides)).toEqual([]);
    });
  });

  describe('shiftIso', () => {
    it('сдвигает дату на N дней без TZ-сдвига', () => {
      expect(shiftIso('2026-04-12', -7)).toBe('2026-04-05');
      expect(shiftIso('2026-04-12', 0)).toBe('2026-04-12');
      expect(shiftIso('2026-02-26', 3)).toBe('2026-03-01');
    });
  });

  describe('computeTodayPopupItems', () => {
    const easter = holiday('Пасха', '2026-04-12', true); // major → [0]
    const minorSaint = holiday('Ильин день', '2026-08-02', false); // → []

    it('в день крупного праздника показывает его (d=0)', () => {
      const items = computeTodayPopupItems([easter], new Map(), '2026-04-12');
      expect(items).toHaveLength(1);
      expect(items[0].holiday.name).toBe('Пасха');
      expect(items[0].leadDays).toBe(0);
    });

    it('мелкий праздник по умолчанию не всплывает', () => {
      const items = computeTodayPopupItems([minorSaint], new Map(), '2026-08-02');
      expect(items).toHaveLength(0);
    });

    it('срабатывает за N дней, если юзер включил', () => {
      const overrides = buildOverridesMap([
        { holidayKey: 'Пасха', leadDays: [0, 7] },
      ]);
      // За 7 дней до Пасхи (12.04) — это 05.04.
      const items = computeTodayPopupItems([easter], overrides, '2026-04-05');
      expect(items).toHaveLength(1);
      expect(items[0].leadDays).toBe(7);
    });

    it('вне окна напоминания — ничего', () => {
      const items = computeTodayPopupItems([easter], new Map(), '2026-04-01');
      expect(items).toHaveLength(0);
    });
  });
});
