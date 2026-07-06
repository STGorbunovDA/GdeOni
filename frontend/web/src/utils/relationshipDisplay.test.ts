import { describe, it, expect } from 'vitest';
import { relationshipDisplay } from './relationshipDisplay';

describe('relationshipDisplay', () => {
  it.each([
    ['Parent', 'Родитель'],
    ['Grandparent', 'Бабушка/дедушка'],
    ['Child', 'Ребёнок'],
    ['Spouse', 'Супруг(а)'],
    ['Sibling', 'Брат/сестра'],
    ['Relative', 'Родственник'],
    ['Friend', 'Друг'],
    ['Acquaintance', 'Знакомый'],
    ['Other', 'Другое'],
  ])('maps %s → %s', (key, expected) => {
    expect(relationshipDisplay(key)).toBe(expected);
  });

  it('returns unknown value as-is', () => {
    expect(relationshipDisplay('Nemesis')).toBe('Nemesis');
  });
});
