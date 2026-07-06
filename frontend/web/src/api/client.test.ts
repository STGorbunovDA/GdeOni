import { describe, it, expect } from 'vitest';
import { ApiError, unwrap } from './client';

/**
 * F19. Тесты unwrap + ApiError: конвертация ApiEnvelope → результат или
 * исключение. Реальные axios-запросы здесь не гоняем — только чистая
 * логика развёртки.
 */
describe('ApiError', () => {
  it('preserves code and message', () => {
    const err = new ApiError('user.email.taken', 'Такой email уже занят.');
    expect(err.code).toBe('user.email.taken');
    expect(err.message).toBe('Такой email уже занят.');
    expect(err.name).toBe('ApiError');
    expect(err).toBeInstanceOf(Error);
  });

  it('is catchable via instanceof', () => {
    try {
      throw new ApiError('x', 'y');
    } catch (e) {
      expect(e).toBeInstanceOf(ApiError);
      expect((e as ApiError).code).toBe('x');
    }
  });
});

describe('unwrap', () => {
  it('returns result when envelope is successful', async () => {
    const payload = { data: { result: { id: 1 }, errorCode: null, errorMessage: null } };
    const value = await unwrap(Promise.resolve(payload));
    expect(value).toEqual({ id: 1 });
  });

  it('throws ApiError when envelope has errorCode', async () => {
    const payload = {
      data: {
        result: null,
        errorCode: 'user.invalid.credentials',
        errorMessage: 'Неверный email или пароль.',
      },
    };
    await expect(unwrap(Promise.resolve(payload))).rejects.toThrow(ApiError);
    await expect(unwrap(Promise.resolve(payload))).rejects.toMatchObject({
      code: 'user.invalid.credentials',
      message: 'Неверный email или пароль.',
    });
  });

  it('uses errorCode as message when errorMessage is null', async () => {
    const payload = {
      data: {
        result: null,
        errorCode: 'some.error',
        errorMessage: null,
      },
    };
    await expect(unwrap(Promise.resolve(payload))).rejects.toMatchObject({
      code: 'some.error',
      message: 'some.error',
    });
  });

  it('throws when result=null and no errorCode (protocol violation)', async () => {
    const payload = {
      data: { result: null, errorCode: null, errorMessage: null },
    };
    await expect(unwrap(Promise.resolve(payload))).rejects.toThrow(ApiError);
    await expect(unwrap(Promise.resolve(payload))).rejects.toMatchObject({
      code: 'unknown',
    });
  });

  it('accepts falsy but valid result values (0, empty string, false)', async () => {
    const zero = { data: { result: 0, errorCode: null, errorMessage: null } };
    await expect(unwrap(Promise.resolve(zero))).resolves.toBe(0);

    const emptyStr = { data: { result: '', errorCode: null, errorMessage: null } };
    await expect(unwrap(Promise.resolve(emptyStr))).resolves.toBe('');

    const falseVal = { data: { result: false, errorCode: null, errorMessage: null } };
    await expect(unwrap(Promise.resolve(falseVal))).resolves.toBe(false);
  });
});
