/**
 * F3. Базовые DTO, общие для всех ответов backend.
 * Соответствуют ApiResponse<T> из backend/src/GdeOni.API/Extensions/ResponseExtensions.cs.
 *
 * Точные эндпоинт-DTO добавляются в src/api/endpoints/<feature>/types.ts
 * по мере появления F4-F17.
 */

/**
 * Стандартная обёртка успешного ответа: { result, errorCode, errorMessage, errors, timeGenerated }.
 * Все поля кроме result обычно null/undefined при HTTP 200.
 * При ошибочных кодах (4xx/5xx) бэк отдаёт ту же оболочку с заполнёнными errorCode + errorMessage.
 */
export type ApiEnvelope<T> = {
  result: T | null;
  errorCode: string | null;
  errorMessage: string | null;
  errors: ValidationErrorDetail[] | null;
  timeGenerated: string;
};

/** Деталь ошибки валидации FluentValidation. */
export type ValidationErrorDetail = {
  propertyName: string;
  errorMessage: string;
  attemptedValue?: unknown;
};

/**
 * Пагинированный ответ. Соответствует PagedResponse<T> на бэке.
 * Все list-эндпоинты с пагинацией возвращают этот тип внутри ApiEnvelope.
 */
export type PagedResponse<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

/**
 * Login/Refresh ответ. Соответствует AuthTokensResponse на бэке.
 * Используется в F3 для refresh interceptor и в F4 для login flow.
 */
export type AuthTokensResponse = {
  accessToken: string;
  refreshToken: string;
  /** ISO-8601 UTC. Поле опциональное — бэк может его не отдавать. */
  expiresAtUtc?: string;
};

/**
 * Минимальный профиль текущего юзера, который F3 использует для проверки сессии.
 * Полный профиль (с подпиской, флагами, etc.) появится в F16.
 */
export type CurrentUserSummary = {
  id: string;
  email: string;
  userName: string;
  role: 'User' | 'Admin' | 'SuperAdmin';
};
