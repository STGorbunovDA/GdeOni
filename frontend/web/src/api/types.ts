/**
 * F3 / F4. Базовые DTO, общие для всех ответов backend.
 * Соответствуют ApiResponse<T> из backend/src/GdeOni.API/Extensions/ResponseExtensions.cs.
 *
 * Точные эндпоинт-DTO добавляются в src/api/endpoints/<feature>/types.ts
 * по мере появления F4-F17.
 */

export type ApiEnvelope<T> = {
  result: T | null;
  errorCode: string | null;
  errorMessage: string | null;
  errors: ValidationErrorDetail[] | null;
  timeGenerated: string;
};

export type ValidationErrorDetail = {
  propertyName: string;
  errorMessage: string;
  attemptedValue?: unknown;
};

export type PagedResponse<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

/**
 * Ответ POST /api/auth/login. Соответствует LoginResponse на бэке
 * (backend/src/GdeOni.Application/Auth/Login/Model/LoginResponse.cs).
 *
 * Важно: ответ содержит ВСЁ что нужно для заполнения store за один
 * запрос — отдельный GET /users/me после login делать не надо.
 */
export type LoginResponse = {
  id: string;
  email: string;
  userName: string;
  fullName: string | null;
  role: 'User' | 'Admin' | 'SuperAdmin';
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  // D45. Подтверждён ли email. false бывает только у «старых» юзеров
  // (новых до подтверждения гейт входа не пускает) — драйвит баннер.
  isEmailConfirmed: boolean;
};

/**
 * Ответ POST /api/auth/refresh. Контракт уже зеркало LoginResponse
 * (см. RefreshResponse на бэке), но мы используем только токены —
 * остальное остаётся неизменным в store.
 */
export type RefreshResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
};

/**
 * Ответ POST /api/users (register). Токены НЕ возвращаются.
 *
 * D45. requiresEmailConfirmation=true → вход закрыт до подтверждения
 * email: фронт показывает экран «проверьте почту» вместо авто-логина.
 * false (dev без SMTP) → можно логиниться сразу, как раньше.
 */
export type RegisterResponse = {
  id: string;
  requiresEmailConfirmation: boolean;
};

/**
 * Ответ GET /api/users/me. Соответствует GetCurrentUserResponse.
 * Используется при startup-refresh для подтягивания актуальной
 * роли и legal-флагов.
 */
export type CurrentUserResponse = {
  id: string;
  email: string;
  userName: string;
  fullName: string | null;
  // Город пользователя. null/пусто → баннер «укажите город» + поле в профиле.
  city: string | null;
  role: 'User' | 'Admin' | 'SuperAdmin';
  privacyPolicyVersion: number;
  termsVersion: number;
  hasOutdatedLegalAcceptance: boolean;
  // D45. false → показать баннер «Подтвердите email». Внутрь приложения
  // неподтверждёнными попадают только «старые» пользователи.
  isEmailConfirmed: boolean;
  // Функция «Родственники»: согласие быть видимым как родственник и получать
  // сообщения (по умолчанию true). Переключатель — в профиле.
  allowRelativeConnections: boolean;
};
