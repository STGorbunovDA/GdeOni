import { describe, expect, it } from 'vitest';
import { mergeAutofilled } from './addressAutofill';

describe('mergeAutofilled', () => {
  it('заполняет пустое поле', () => {
    expect(mergeAutofilled('', '', 'Москва')).toBe('Москва');
  });

  it('обновляет значение, которое сами же подставили раньше', () => {
    // Юзер подвинул точку с Твери на Москву — город должен переехать.
    expect(mergeAutofilled('Тверь', 'Тверь', 'Москва')).toBe('Москва');
  });

  it('НЕ трогает поле, которое юзер правил руками', () => {
    // Определилось «Мытищи», но человек вписал «Москва» — его слово главнее.
    expect(mergeAutofilled('Москва', 'Мытищи', 'Химки')).toBe('Москва');
  });

  it('оставляет поле как есть, если геокодер ничего не вернул', () => {
    expect(mergeAutofilled('Москва', 'Москва', null)).toBe('Москва');
    expect(mergeAutofilled('', '', null)).toBe('');
  });

  it('не считает пробелы за ручной ввод', () => {
    expect(mergeAutofilled('   ', '', 'Москва')).toBe('Москва');
    expect(mergeAutofilled(' Тверь ', 'Тверь', 'Москва')).toBe('Москва');
  });
});
