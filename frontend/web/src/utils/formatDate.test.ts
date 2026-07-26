import { describe, it, expect } from 'vitest';
import {
  formatDateOnly,
  formatDateTime,
  formatRuDate,
  maskDateInput,
  parseDateInputValue,
  parseRuDate,
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

  describe('maskDateInput (авто-точки ДД.ММ.ГГГГ)', () => {
    it('расставляет точки по мере ввода цифр', () => {
      expect(maskDateInput('0')).toBe('0');
      expect(maskDateInput('01')).toBe('01');
      expect(maskDateInput('010')).toBe('01.0');
      expect(maskDateInput('0101')).toBe('01.01');
      expect(maskDateInput('01011')).toBe('01.01.1');
      expect(maskDateInput('01011990')).toBe('01.01.1990');
    });

    it('игнорирует нецифры и лишние символы (можно вставлять готовую дату)', () => {
      expect(maskDateInput('01.01.1990')).toBe('01.01.1990');
      expect(maskDateInput('01/01/1990')).toBe('01.01.1990');
      expect(maskDateInput('abc01def01')).toBe('01.01');
    });

    it('обрезает лишние цифры после 8', () => {
      expect(maskDateInput('010119901234')).toBe('01.01.1990');
    });

    it('пустая строка → пустая', () => {
      expect(maskDateInput('')).toBe('');
    });
  });

  describe('parseRuDate (строгий разбор ДД.ММ.ГГГГ)', () => {
    it('парсит полную валидную дату без таймзонного сдвига', () => {
      const d = parseRuDate('08.03.1987')!;
      expect(d).not.toBeNull();
      expect(d.getFullYear()).toBe(1987);
      expect(d.getMonth()).toBe(2); // март
      expect(d.getDate()).toBe(8);
    });

    it('не мапит годы 0-99 в 1900+год', () => {
      expect(parseRuDate('11.11.0001')!.getFullYear()).toBe(1);
    });

    it('отбрасывает несуществующие и неполные даты', () => {
      expect(parseRuDate('31.02.2020')).toBeNull(); // 31 февраля
      expect(parseRuDate('01.13.1990')).toBeNull(); // 13-й месяц
      expect(parseRuDate('00.01.1990')).toBeNull(); // день 0
      expect(parseRuDate('01.01')).toBeNull(); // недобор
      expect(parseRuDate('1.1.1990')).toBeNull(); // без ведущих нулей
      expect(parseRuDate('')).toBeNull();
    });
  });

  describe('formatRuDate (Date → ДД.ММ.ГГГГ)', () => {
    it('форматирует с ведущими нулями', () => {
      expect(formatRuDate(new Date(1990, 0, 5))).toBe('05.01.1990');
    });

    it('round-trip через parseRuDate без потерь', () => {
      const d = parseRuDate('08.03.1987');
      expect(formatRuDate(d)).toBe('08.03.1987');
    });

    it('null/undefined → пустая строка', () => {
      expect(formatRuDate(null)).toBe('');
      expect(formatRuDate(undefined)).toBe('');
    });
  });
});
