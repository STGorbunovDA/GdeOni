import axios, {
  AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';
import { useAuthStore } from '../auth/authStore';
import { API_BASE_URL } from './config';
import { refreshTokens } from './refreshClient';

/**
 * F3. Главный axios-instance. Зеркало AuthTokenHandler +
 * RefreshTokenHandler на mobile.
 *
 *  - Request interceptor: добавляет Authorization: Bearer <access>.
 *  - Response interceptor: при 401 пытается refresh, ретраит исходный
 *    запрос с новым токеном; при провале — logout + redirect /login.
 *
 * Параллельные 401 шарят одну Promise refresh'а (singleflight), чтобы
 * не было N одновременных refresh'ей при одновременной просрочке
 * токена и N запросах на странице.
 */
export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
});

/* ───────── Request: Authorization ───────── */

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

/* ───────── Response: 401 → refresh → retry ───────── */

/**
 * Маркер на конфиге запроса, чтобы после успешного refresh не зацикливаться.
 * AxiosRequestConfig можно расширять — TypeScript declaration merge.
 */
type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean };

/**
 * Текущая активная Promise refresh'а. Если несколько запросов одновременно
 * получили 401 — они все ждут эту одну Promise. После её разрешения каждый
 * сам ретраит свой запрос с новым токеном.
 */
let refreshInFlight: Promise<string | null> | null = null;

function performRefresh(): Promise<string | null> {
  if (refreshInFlight) return refreshInFlight;

  const currentRefresh = useAuthStore.getState().refreshToken;
  if (!currentRefresh) return Promise.resolve(null);

  refreshInFlight = refreshTokens(currentRefresh)
    .then((tokens) => {
      if (!tokens) return null;
      // Роль не приходит в /auth/refresh — берём текущую из store
      // (она не меняется при refresh; ChangeRole на бэке инвалидирует
      // SecurityStamp и тогда refresh упадёт раньше).
      const currentRole = useAuthStore.getState().role ?? 'User';
      useAuthStore
        .getState()
        .setSession(tokens.accessToken, tokens.refreshToken, currentRole);
      return tokens.accessToken;
    })
    .finally(() => {
      refreshInFlight = null;
    });

  return refreshInFlight;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetriableConfig | undefined;

    // Сетевая ошибка / таймаут / запрос отменён — пробрасываем как есть.
    if (!error.response || !original) {
      return Promise.reject(error);
    }

    // 401 и это НЕ повторная попытка → пробуем refresh.
    if (error.response.status === 401 && !original._retried) {
      original._retried = true;

      const newAccess = await performRefresh();
      if (!newAccess) {
        // Refresh упал → logout + редирект.
        useAuthStore.getState().clear();
        // Не используем react-router здесь — это axios layer, не React.
        // window.location форсит full reload, что также сбрасывает все
        // TanStack Query кэши. Это ок при logout.
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }

      // Подставляем новый токен в исходный конфиг и ретраим.
      original.headers.set('Authorization', `Bearer ${newAccess}`);
      return apiClient.request(original);
    }

    return Promise.reject(error);
  },
);

/**
 * Helper: удобная развёртка ApiEnvelope<T>. Использовать так:
 *   const data = await unwrap(apiClient.get<ApiEnvelope<MyResp>>('/api/...'));
 * Если бэк вернул успешный HTTP-код, но с errorCode — кидаем ApiError.
 */
export class ApiError extends Error {
  constructor(
    public readonly code: string,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export async function unwrap<T>(
  promise: Promise<{ data: { result: T | null; errorCode: string | null; errorMessage: string | null } }>,
): Promise<T> {
  const { data } = await promise;
  if (data.errorCode) {
    throw new ApiError(data.errorCode, data.errorMessage ?? data.errorCode);
  }
  if (data.result === null) {
    throw new ApiError('unknown', 'Backend вернул result=null без errorCode.');
  }
  return data.result;
}

export type { AxiosRequestConfig };
