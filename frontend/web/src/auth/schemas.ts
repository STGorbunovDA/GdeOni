import { z } from 'zod';

/**
 * F4. Zod-схемы для login/register. Зеркало:
 *  - backend/src/GdeOni.Application/Constants/PasswordPolicy.cs
 *    (MinPasswordLength=8, MaxPasswordLength=128).
 *  - backend RegisterUserCommandValidator (email, password, etc.).
 *
 * Лимиты сознательно слабее, чем у бэка (только базовые проверки
 * формы). Дополнительные правила (например, что email уникален) —
 * бэк проверит и вернёт errorCode, который покажем в форме.
 */

export const MIN_PASSWORD_LENGTH = 8;
export const MAX_PASSWORD_LENGTH = 128;

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'Введите email')
    .email('Невалидный email'),
  password: z
    .string()
    .min(MIN_PASSWORD_LENGTH, `Пароль не короче ${MIN_PASSWORD_LENGTH} символов`)
    .max(MAX_PASSWORD_LENGTH, `Пароль не длиннее ${MAX_PASSWORD_LENGTH} символов`),
});

export type LoginFormValues = z.infer<typeof loginSchema>;

export const registerSchema = z
  .object({
    email: z.string().min(1, 'Введите email').email('Невалидный email'),
    password: z
      .string()
      .min(MIN_PASSWORD_LENGTH, `Пароль не короче ${MIN_PASSWORD_LENGTH} символов`)
      .max(MAX_PASSWORD_LENGTH, `Пароль не длиннее ${MAX_PASSWORD_LENGTH} символов`),
    confirmPassword: z.string().min(1, 'Повторите пароль'),
    userName: z
      .string()
      .max(64, 'Имя пользователя не длиннее 64 символов')
      .optional()
      .or(z.literal('')),
    privacyPolicyAccepted: z.literal(true, {
      message: 'Необходимо принять Политику конфиденциальности',
    }),
    termsAccepted: z.literal(true, {
      message: 'Необходимо принять Условия использования',
    }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Пароли не совпадают',
    path: ['confirmPassword'],
  });

export type RegisterFormValues = z.infer<typeof registerSchema>;
