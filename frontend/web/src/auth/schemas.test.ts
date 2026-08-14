import { describe, it, expect } from 'vitest';
import {
  loginSchema,
  registerSchema,
  changePasswordSchema,
  MIN_ALLOWED_AGE,
} from './schemas';

/**
 * F19. Тесты Zod-схем аутентификации. Особый фокус — F24 age-gate:
 * младше MIN_ALLOWED_AGE лет регистрация должна отбиваться.
 */

const validPassword = 'Password123!';

describe('loginSchema', () => {
  it('accepts valid email + password', () => {
    const result = loginSchema.safeParse({
      email: 'user@example.com',
      password: validPassword,
    });
    expect(result.success).toBe(true);
  });

  // Вход принимает email ИЛИ логин, поэтому значение без «@» — валидно.
  // Раньше схема отбивала его как «Невалидный email», и войти под своим
  // логином было невозможно.
  it('accepts login without @', () => {
    const result = loginSchema.safeParse({
      email: 'ivan_petrov',
      password: validPassword,
    });
    expect(result.success).toBe(true);
  });

  it('rejects empty email or login', () => {
    const result = loginSchema.safeParse({
      email: '',
      password: validPassword,
    });
    expect(result.success).toBe(false);
  });

  it('rejects short password', () => {
    const result = loginSchema.safeParse({
      email: 'user@example.com',
      password: '123',
    });
    expect(result.success).toBe(false);
  });
});

describe('registerSchema', () => {
  const validBase = {
    email: 'user@example.com',
    password: validPassword,
    confirmPassword: validPassword,
    fullName: 'Алиса Иванова',
    birthDate: new Date(new Date().getFullYear() - 20, 0, 1),
    privacyPolicyAccepted: true as const,
    termsAccepted: true as const,
    allowRelativeConnections: true,
  };

  it('accepts valid input', () => {
    const result = registerSchema.safeParse(validBase);
    expect(result.success).toBe(true);
  });

  it('rejects mismatched passwords', () => {
    const result = registerSchema.safeParse({
      ...validBase,
      confirmPassword: 'Different1!',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(
        result.error.issues.some((i) => i.path.includes('confirmPassword')),
      ).toBe(true);
    }
  });

  it('rejects without privacy consent', () => {
    const result = registerSchema.safeParse({
      ...validBase,
      privacyPolicyAccepted: false,
    });
    expect(result.success).toBe(false);
  });

  it('rejects without terms consent', () => {
    const result = registerSchema.safeParse({
      ...validBase,
      termsAccepted: false,
    });
    expect(result.success).toBe(false);
  });

  describe('D19 age gate', () => {
    it('accepts user exactly 14 years old today', () => {
      const today = new Date();
      const exactly14 = new Date(
        today.getFullYear() - MIN_ALLOWED_AGE,
        today.getMonth(),
        today.getDate(),
      );
      const result = registerSchema.safeParse({
        ...validBase,
        birthDate: exactly14,
      });
      expect(result.success).toBe(true);
    });

    it('rejects user one day before 14th birthday', () => {
      const today = new Date();
      const oneDayShort = new Date(
        today.getFullYear() - MIN_ALLOWED_AGE,
        today.getMonth(),
        today.getDate() + 1,
      );
      const result = registerSchema.safeParse({
        ...validBase,
        birthDate: oneDayShort,
      });
      expect(result.success).toBe(false);
    });

    it('rejects 10-year-old', () => {
      const today = new Date();
      const tenYearsOld = new Date(
        today.getFullYear() - 10,
        today.getMonth(),
        today.getDate(),
      );
      const result = registerSchema.safeParse({
        ...validBase,
        birthDate: tenYearsOld,
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(
          result.error.issues.some((i) => i.message.includes(String(MIN_ALLOWED_AGE))),
        ).toBe(true);
      }
    });

    it('rejects future birth date', () => {
      const future = new Date(new Date().getFullYear() + 1, 0, 1);
      const result = registerSchema.safeParse({
        ...validBase,
        birthDate: future,
      });
      expect(result.success).toBe(false);
    });

    it('rejects missing birthDate', () => {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { birthDate: _omit, ...withoutBirth } = validBase;
      const result = registerSchema.safeParse(withoutBirth);
      expect(result.success).toBe(false);
    });
  });
});

describe('changePasswordSchema', () => {
  it('accepts valid current + new password', () => {
    const result = changePasswordSchema.safeParse({
      currentPassword: 'OldPassword1',
      newPassword: validPassword,
      confirmPassword: validPassword,
    });
    expect(result.success).toBe(true);
  });

  it('rejects mismatched new/confirm passwords', () => {
    const result = changePasswordSchema.safeParse({
      currentPassword: 'OldPassword1',
      newPassword: validPassword,
      confirmPassword: 'Different1!',
    });
    expect(result.success).toBe(false);
  });
});
