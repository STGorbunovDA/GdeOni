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

/**
 * F24 / D19. Возрастной гард — зеркало `User.MinAllowedAge` на бэке
 * (Условия использования, п. 3.4). Клиент делает предварительную
 * проверку; финальное слово — за бэком.
 */
export const MIN_ALLOWED_AGE = 14;

/**
 * Нижняя граница года рождения. Не бизнес-правило, а санитарная
 * проверка ввода: отсекает недонабранный год из нативного date-инпута
 * (0001, 0019, 0198 — все они «старше 14 лет» и иначе прошли бы).
 */
export const MIN_ALLOWED_BIRTH_YEAR = 1900;

function calculateAge(birthDate: Date, today: Date): number {
  let age = today.getFullYear() - birthDate.getFullYear();
  const m = today.getMonth() - birthDate.getMonth();
  if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
    age--;
  }
  return age;
}

/**
 * Вход принимает email ИЛИ логин, поэтому проверки `.email()` здесь нет:
 * она отбивала вход по логину («Невалидный email») ещё до запроса к API.
 * Существование учётки проверяет сервер и отвечает единым
 * «Неверный email/логин или пароль».
 */
export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'Введите email или логин'),
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
    // D19. birthDate — обязательное. Валидируем: не в будущем и не
    // младше MIN_ALLOWED_AGE лет. Финальную проверку делает бэк
    // (User.Register + TimeProvider), но UX без front-guard хуже.
    birthDate: z
      .date({ message: 'Укажите дату рождения' })
      // Пока человек набирает год с клавиатуры, браузер отдаёт
      // промежуточные даты (0001-…, 0019-…, 0198-…). Такие значения
      // проходят возрастной гард (им «больше 14 лет»), поэтому нужен
      // отдельный нижний предел — иначе недонабранный год уедет на бэк.
      .refine(
        (d) => d.getFullYear() >= MIN_ALLOWED_BIRTH_YEAR,
        { message: 'Проверьте год рождения' },
      )
      .refine(
        (d) => d <= new Date(),
        { message: 'Дата рождения не может быть в будущем' },
      )
      .refine(
        (d) => calculateAge(d, new Date()) >= MIN_ALLOWED_AGE,
        {
          message: `Сервисом могут пользоваться лица от ${MIN_ALLOWED_AGE} лет`,
        },
      ),
    privacyPolicyAccepted: z.literal(true, {
      message: 'Необходимо принять Политику конфиденциальности',
    }),
    termsAccepted: z.literal(true, {
      message: 'Необходимо принять Условия использования',
    }),
    // Функция «Родственники»: согласие быть видимым как родственник и
    // получать сообщения. НЕ обязательное (в отличие от двух выше) —
    // по умолчанию включено, человек может снять.
    allowRelativeConnections: z.boolean(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Пароли не совпадают',
    path: ['confirmPassword'],
  });

export type RegisterFormValues = z.infer<typeof registerSchema>;

/**
 * F16. Схема смены пароля. Зеркало mobile PasswordRules: current не пуст,
 * new в диапазоне 8..128, confirm == new. Дополнительно бэк проверит
 * `user.current_password.invalid` → текст подставит formatError.
 */
export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Введите текущий пароль'),
    newPassword: z
      .string()
      .min(MIN_PASSWORD_LENGTH, `Пароль не короче ${MIN_PASSWORD_LENGTH} символов`)
      .max(MAX_PASSWORD_LENGTH, `Пароль не длиннее ${MAX_PASSWORD_LENGTH} символов`),
    confirmPassword: z.string().min(1, 'Повторите новый пароль'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Пароли не совпадают',
    path: ['confirmPassword'],
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;

/**
 * D43. Запрос ссылки восстановления — только email.
 */
export const forgotPasswordSchema = z.object({
  email: z.string().min(1, 'Введите email').email('Невалидный email'),
});

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;

/**
 * D43. Установка нового пароля по ссылке из письма. Текущий пароль не
 * спрашиваем — человек его как раз и не помнит; подтверждением личности
 * служит токен из письма.
 */
export const resetPasswordSchema = z
  .object({
    newPassword: z
      .string()
      .min(MIN_PASSWORD_LENGTH, `Пароль не короче ${MIN_PASSWORD_LENGTH} символов`)
      .max(MAX_PASSWORD_LENGTH, `Пароль не длиннее ${MAX_PASSWORD_LENGTH} символов`),
    confirmPassword: z.string().min(1, 'Повторите новый пароль'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Пароли не совпадают',
    path: ['confirmPassword'],
  });

export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;
