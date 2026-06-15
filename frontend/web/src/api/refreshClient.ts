import axios from 'axios';
import { API_BASE_URL } from './config';
import type { ApiEnvelope, AuthTokensResponse } from './types';

/**
 * F3. Отдельный axios-instance исключительно для POST /api/auth/refresh.
 * НЕТ interceptors — иначе при провале refresh interceptor попытается
 * сделать ещё один refresh, и так до StackOverflow. Mobile-аналог —
 * RefreshHttpClientProvider в AuthTokenHandler.
 */
const refreshAxios = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
});

/**
 * Дергает POST /api/auth/refresh с текущим refresh-токеном.
 * Возвращает новые пары токенов либо null при провале (revoked / expired).
 *
 * Бэк отдаёт 200 + ApiEnvelope<AuthTokensResponse> при успехе либо
 * 401/409 при провале (refresh уже использован, заблокирован юзер, etc.).
 */
export async function refreshTokens(
  refreshToken: string,
): Promise<AuthTokensResponse | null> {
  try {
    const { data } = await refreshAxios.post<ApiEnvelope<AuthTokensResponse>>(
      '/api/auth/refresh',
      { refreshToken },
    );
    return data.result ?? null;
  } catch {
    return null;
  }
}
