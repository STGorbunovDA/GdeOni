import { describe, it, expect } from 'vitest';
import {
  formatDateOnly,
  formatDateTime,
  parseDateInputValue,
  toDateInputValue,
} from './formatDate';

describe('formatDate utils', () => {
  describe('formatDateOnly', () => {
    it('converts ISO date to dd.MM.yyyy', () => {
      expect(formatDateOnly('2026-07-06')).toBe('06.07.2026');
    });

    it('preserves leading zeros', () => {
      expect(formatDateOnly('2000-01-05')).toBe('05.01.2000');
    });
  });

  describe('formatDateTime', () => {
    it('produces dd.MM.yyyy HH:mm without timezone drift', () => {
      // Формат зависит от локали, но структура «дата пробел время»
      // должна быть стабильной.
      const result = formatDateTime('2026-07-06T12:34:56Z');
      expect(result).toMatch(/^\d{2}\.\d{2}\.\d{4}\s\d{2}:\d{2}$/);
    });
  });

  /**
   * Round-trip `input → parse → Date → format → input` обязан быть без
   * потерь. Поля дат контролируемые: если формат вернёт не то, что
   * пришло, React запишет в инпут чужое значение и собьёт человеку ввод.
   *
   * Регрессия: юзер набирал год 1987 и получал 1901 без возможности
   * исправить. Пока год недонабран, браузер отдаёт полные даты
   * «0001-…», «0019-…», «0198-…» — именно они и ломались.
   */
  describe('round-trip date-инпута', () => {
    const partialYears = [
      '0001-11-11',
      '0019-11-11',
      '0198-11-11',
      '1987-11-11',
    ];

    it.each(partialYears)('%s переживает round-trip без изменений', (raw) => {
      const parsed = parseDateInputValue(raw);
      expect(parsed).not.toBeNull();
      expect(toDateInputValue(parsed)).toBe(raw);
    });

    it('не превращает годы 0-99 в 1900+год', () => {
      // Корень бага: new Date(1, 10, 11) даёт 1901 год.
      expect(parseDateInputValue('0001-11-11')!.getFullYear()).toBe(1);
      expect(parseDateInputValue('0099-06-15')!.getFullYear()).toBe(99);
    });

    it('добивает год нулями до 4 знаков', () => {
      // «1-11-11» для <input type="date"> невалиден.
      const d = new Date(2000, 10, 11);
      d.setFullYear(1);
      expect(toDateInputValue(d)).toBe('0001-11-11');
    });

    it('сохраняет обычную дату и не уезжает по таймзоне', () => {
      expect(toDateInputValue(parseDateInputValue('1987-03-08'))).toBe(
        '1987-03-08',
      );
      expect(parseDateInputValue('1987-03-08')!.getDate()).toBe(8);
    });

    it('пустую и битую строку отдаёт как null', () => {
      expect(parseDateInputValue('')).toBeNull();
      expect(parseDateInputValue('не-дата')).toBeNull();
      expect(toDateInputValue(null)).toBe('');
      expect(toDateInputValue(undefined)).toBe('');
    });
  });
});
