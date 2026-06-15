import { useEffect, type ReactNode } from 'react';
import { useAuthStore } from './authStore';
import { refreshTokens } from '../api/refreshClient';
import { usersApi } from '../api/endpoints/authApi';

/**
 * F4. Startup-refresh. На монтировании:
 *  - если в store есть refresh-токен → дёргаем /api/auth/refresh,
 *    при успехе дёргаем /api/users/me для подтягивания актуальной
 *    роли (она могла измениться пока юзера не было — ChangeRole
 *    инвалидирует SecurityStamp и refresh-токен тоже становится
 *    невалидным, но если refresh успешен — роль точно актуальная);
 *  - если refresh упал или его нет → чистим store.
 *
 * isBootstrapping в store держится в true пока этот процесс идёт,
 * чтобы ProtectedRoute не редиректил преждевременно. Аналог
 * AppShell.OnAppearing.HasSessionAsync на mobile.
 *
 * bootstrapStarted на module-level: React 18 в StrictMode вызывает
 * useEffect дважды в dev для отлова side-effect багов. Без этого
 * флага бэкенд получает два запроса refresh подряд при каждом старте,
 * это сжирает rate-limit-квоту (10 req/min на /auth).
 */
let bootstrapStarted = false;

export function SessionBootstrap({ children }: { children: ReactNode }) {
  useEffect(() => {
    if (bootstrapStarted) return;
    bootstrapStarted = true;

    let cancelled = false;

    async function bootstrap() {
      const { refreshToken, setTokens, setSession, clear, setBootstrapping } =
        useAuthStore.getState();

      if (!refreshToken) {
        setBootstrapping(false);
        return;
      }

      try {
        const tokens = await refreshTokens(refreshToken);
        if (cancelled) return;
        if (!tokens) {
          clear();
          setBootstrapping(false);
          return;
        }
        // Применяем токены ДО /users/me, чтобы request interceptor
        // подложил свежий access.
        setTokens(tokens.accessToken, tokens.refreshToken);

        const me = await usersApi.me();
        if (cancelled) return;
        setSession(tokens.accessToken, tokens.refreshToken, {
          id: me.id,
          email: me.email,
          userName: me.userName,
          fullName: me.fullName,
          role: me.role,
        });
      } catch {
        if (cancelled) return;
        clear();
      } finally {
        if (!cancelled) setBootstrapping(false);
      }
    }

    bootstrap();
    return () => {
      cancelled = true;
    };
  }, []);

  return <>{children}</>;
}
