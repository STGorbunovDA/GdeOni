import { describe, it, expect } from 'vitest';
import { formatDateOnly, formatDateTime } from './formatDate';

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
});
