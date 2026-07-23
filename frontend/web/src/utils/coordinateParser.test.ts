import { describe, it, expect } from 'vitest';
import {
  tryParseDouble,
  tryParseLatitude,
  tryParseLongitude,
  tryParseAccuracy,
} from './coordinateParser';

/**
 * F19. Тесты парсера координат — зеркало
 * GdeOni.Mobile.Shared.Tests/CoordinateParserTests.cs.
 */
describe('coordinateParser', () => {
  describe('tryParseDouble', () => {
    it('parses point notation', () => {
      expect(tryParseDouble('55.7558')).toBe(55.7558);
    });

    it('parses comma notation (Windows / RU locale)', () => {
      expect(tryParseDouble('55,7558')).toBe(55.7558);
    });

    it('trims surrounding whitespace', () => {
      expect(tryParseDouble('  55.7558  ')).toBe(55.7558);
    });

    it('parses negative values', () => {
      expect(tryParseDouble('-37.6173')).toBe(-37.6173);
    });

    it.each([null, undefined, '', '   '])(
      'returns null for empty-like input %j',
      (input) => {
        expect(tryParseDouble(input)).toBeNull();
      },
    );

    it('returns null for non-numeric input', () => {
      expect(tryParseDouble('abc')).toBeNull();
    });
  });

  describe('tryParseLatitude', () => {
    it('accepts valid latitude', () => {
      expect(tryParseLatitude('55.7558')).toBe(55.7558);
    });

    it('accepts boundary values -90 and 90', () => {
      expect(tryParseLatitude('-90')).toBe(-90);
      expect(tryParseLatitude('90')).toBe(90);
    });

    it('rejects out-of-range values', () => {
      expect(tryParseLatitude('90.0001')).toBeNull();
      expect(tryParseLatitude('-90.5')).toBeNull();
    });
  });

  describe('tryParseLongitude', () => {
    it('accepts valid longitude', () => {
      expect(tryParseLongitude('37.6173')).toBe(37.6173);
    });

    it('accepts boundary values -180 and 180', () => {
      expect(tryParseLongitude('-180')).toBe(-180);
      expect(tryParseLongitude('180')).toBe(180);
    });

    it('rejects out-of-range values', () => {
      expect(tryParseLongitude('180.01')).toBeNull();
      expect(tryParseLongitude('-180.5')).toBeNull();
    });
  });

  describe('tryParseAccuracy', () => {
    it('accepts positive values', () => {
      expect(tryParseAccuracy('12.5')).toBe(12.5);
    });

    it('accepts zero', () => {
      expect(tryParseAccuracy('0')).toBe(0);
    });

    it('rejects negative values', () => {
      expect(tryParseAccuracy('-1')).toBeNull();
    });
  });
});
