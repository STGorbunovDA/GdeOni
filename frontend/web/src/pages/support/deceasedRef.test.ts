import { describe, it, expect } from 'vitest';
import { extractDeceasedRefId } from './deceasedRef';

/**
 * F34. Проверяем, что регэксп парсит маркер «ID карточки: {guid}» из
 * Description тикета: pre-fill'ится SupportNewPage при переходе с
 * карточки умершего. Должен быть 1-в-1 с mobile
 * SupportDeceasedRefParser.
 */
describe('extractDeceasedRefId', () => {
  const guid = 'a3b8fbe9-1a58-4d51-9e26-3f0c1a3b0f00';

  it('extracts guid from typical template', () => {
    const desc = [
      'Карточка умершего: Иван Иванов',
      'Жизнь: 1950 — 2020',
      `ID карточки: ${guid}`,
      '',
      '---',
      '',
      'Опишите проблему ниже:',
      'Тут проблема.',
    ].join('\n');
    expect(extractDeceasedRefId(desc)).toBe(guid);
  });

  it('is case-insensitive to guid hex chars, normalizes to lower', () => {
    const upper = 'A3B8FBE9-1A58-4D51-9E26-3F0C1A3B0F00';
    expect(extractDeceasedRefId(`ID карточки: ${upper}`)).toBe(upper.toLowerCase());
  });

  it('tolerates multiple spaces before guid', () => {
    expect(extractDeceasedRefId(`ID карточки:   ${guid}`)).toBe(guid);
  });

  it('returns null when marker is missing', () => {
    expect(extractDeceasedRefId('Просто описание без маркера.')).toBeNull();
  });

  it('returns null on invalid guid (missing dashes)', () => {
    expect(extractDeceasedRefId('ID карточки: not-a-guid-really')).toBeNull();
  });

  it('returns null on empty / null / undefined description', () => {
    expect(extractDeceasedRefId('')).toBeNull();
    expect(extractDeceasedRefId(null)).toBeNull();
    expect(extractDeceasedRefId(undefined)).toBeNull();
  });

  it('picks the first guid when multiple are present', () => {
    const first = 'aaaaaaaa-1111-2222-3333-444444444444';
    const second = 'bbbbbbbb-5555-6666-7777-888888888888';
    const desc = `ID карточки: ${first}\nID карточки: ${second}`;
    expect(extractDeceasedRefId(desc)).toBe(first);
  });
});
