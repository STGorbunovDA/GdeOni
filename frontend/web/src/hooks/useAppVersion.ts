import { useQuery } from '@tanstack/react-query';
import { appApi, type AppVersion } from '../api/endpoints/appApi';

/**
 * F22 / D17. Тянет /api/app/version при старте приложения.
 * AllowAnonymous на бэке — работает и без авторизации, чтобы
 * можно было показать блокирующую модалку "обновите страницу"
 * даже когда refresh упал.
 *
 * staleTime=∞ — версия проверяется один раз на сессию.
 */
export function useAppVersion(): {
  data: AppVersion | undefined;
  isLoading: boolean;
  isError: boolean;
} {
  const query = useQuery({
    queryKey: ['app-version'],
    queryFn: () => appApi.version(),
    staleTime: Infinity,
    gcTime: Infinity,
    retry: 1,
  });

  return {
    data: query.data,
    isLoading: query.isLoading,
    isError: query.isError,
  };
}

/**
 * Web-версия приложения — это commit SHA или build timestamp,
 * подставляемый Vite через `define`/`VITE_APP_VERSION`.
 *
 * fallback 'dev' используется в локальной разработке, когда
 * скрипта, подставляющего SHA, ещё нет.
 */
export const CURRENT_APP_VERSION: string =
  import.meta.env.VITE_APP_VERSION ?? 'dev';
