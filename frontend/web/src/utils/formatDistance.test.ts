import { describe, expect, it } from 'vitest';
import { formatDistance } from './formatDistance';

describe('formatDistance', () => {
  it('метры показывает целыми', () => {
    expect(formatDistance(0)).toBe('0 м');
    expect(formatDistance(120)).toBe('120 м');
    expect(formatDistance(999)).toBe('999 м');
  });

  it('от километра переключается на км', () => {
    expect(formatDistance(1000)).toBe('1 км');
    expect(formatDistance(1200)).toBe('1.2 км');
    expect(formatDistance(4560)).toBe('4.6 км');
  });

  it('отрицательные и мусорные значения схлопывает в 0 м', () => {
    expect(formatDistance(-5)).toBe('0 м');
    expect(formatDistance(Number.NaN)).toBe('0 м');
  });

  it('округляет дробные метры', () => {
    expect(formatDistance(120.4)).toBe('120 м');
    expect(formatDistance(120.6)).toBe('121 м');
  });
});
