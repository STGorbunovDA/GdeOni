import { describe, it, expect } from 'vitest';
import { AxiosError, AxiosHeaders } from 'axios';
import { formatError } from './errorMessages';
import { ApiError } from '../api/client';

/**
 * F19. Тесты formatError — критично для UX: неправильный код или
 * необёрнутый AxiosError показывает юзеру мусор.
 */

function makeAxiosError(status: number, errorCode: string | null): AxiosError {
  const config = { headers: new AxiosHeaders() } as never;
  const err = new AxiosError(
    'Request failed',
    'ERR_BAD_REQUEST',
    config,
    {},
    {
      status,
      data: { errorCode, errorMessage: null },
      statusText: '',
      headers: {},
      config,
    },
  );
  return err;
}

describe('formatError', () => {
  describe('ApiError', () => {
    it('maps known code to Russian message', () => {
      const err = new ApiError('user.invalid.credentials', 'x');
      expect(formatError(err)).toBe('Неверный email/логин или пароль.');
    });

    it('falls back to error.message for unknown code', () => {
      const err = new ApiError('some.unknown.code', 'raw text');
      expect(formatError(err)).toBe('raw text');
    });
  });

  describe('AxiosError with envelope errorCode', () => {
    it('maps envelope code to Russian message', () => {
      const err = makeAxiosError(400, 'user.email.already.exists');
      expect(formatError(err)).toBe(
        'Пользователь с таким email уже существует.',
      );
    });

    it('handles new codes from F24', () => {
      const err = makeAxiosError(400, 'user.privacy_policy.not_accepted');
      expect(formatError(err)).toContain('Политику');
    });
  });

  describe('AxiosError without envelope', () => {
    it('429 → too many attempts', () => {
      const err = makeAxiosError(429, null);
      expect(formatError(err)).toContain('Слишком много попыток');
    });

    it('401 → invalid credentials', () => {
      const err = makeAxiosError(401, null);
      expect(formatError(err)).toBe('Неверный email/логин или пароль.');
    });

    it('403 → no access', () => {
      const err = makeAxiosError(403, null);
      expect(formatError(err)).toContain('нет доступа');
    });

    it('unknown 5xx → generic network error', () => {
      const err = makeAxiosError(500, null);
      expect(formatError(err)).toContain('HTTP 500');
    });
  });

  describe('network / generic', () => {
    it('AxiosError with ERR_NETWORK → server unreachable', () => {
      const config = { headers: new AxiosHeaders() } as never;
      const err = new AxiosError(
        'Network',
        'ERR_NETWORK',
        config,
        {},
      );
      expect(formatError(err)).toContain('соединиться');
    });

    it('plain Error with message → propagates message', () => {
      expect(formatError(new Error('custom throw'))).toBe('custom throw');
    });

    it('unknown / null → generic message', () => {
      expect(formatError(null)).toContain('Неизвестная');
      expect(formatError({} as unknown)).toContain('Неизвестная');
    });
  });
});
